using BitwardenApi.Notifications.Abstractions;
using System.Security.Cryptography;
using BitwardenApi.Vault.Attachments;
using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Vault.Services;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Vault;

public sealed class VaultCipherAttachmentServiceTests
{
    [Fact]
    public async Task Download_decrypts_the_selected_organization_cipher_and_attachment_to_the_requested_path()
    {
        using var context = new VaultManagerTestContext();
        using var destination = new TemporaryVaultAttachmentFile();
        OrganizationId organizationId = OrganizationId.Parse("11111111-1111-1111-1111-111111111111");
        CipherId cipherId = CipherId.Parse("attachment-cipher");
        AttachmentId attachmentId = AttachmentId.Parse("attachment-file");
        byte[] userKey = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        byte[] organizationKey = Enumerable.Range(65, 64).Select(static value => (byte)value).ToArray();
        byte[] cipherKey = Enumerable.Range(129, 64).Select(static value => (byte)value).ToArray();
        byte[] attachmentKey = Enumerable.Range(193, 64).Select(static value => (byte)value).ToArray();
        byte[] plaintext = "synthetic attachment plaintext"u8.ToArray();
        using var accountKeys = new AccountKeySessionTestContext(userKey);
        Assert.Equal(context.Account.UserId, accountKeys.UserId);
        EncString protectedCipherKey = EncString.Encrypt(cipherKey, organizationKey);
        EncString protectedAttachmentKey = EncString.Encrypt(attachmentKey, cipherKey);
        AsymmetricEncString protectedOrganizationKey = accountKeys.EncryptOrganizationKey(organizationKey);
        accountKeys.Session.UpdateOrganizations(new Dictionary<OrganizationId, AsymmetricEncString>
        {
            [organizationId] = protectedOrganizationKey
        });
        byte[] encryptedFile = VaultCipherAttachmentTestData.EncryptEncFile(plaintext, attachmentKey);
        var organization = new VaultOrganizationResponse
        {
            Id = organizationId,
            UserId = context.Account.UserId,
            OrganizationUserId = Guid.Parse("12345678-1234-5678-9abc-def012345678"),
            Name = "Attachment organization",
            Status = 2,
            Enabled = true,
            ProtectedOrganizationKey = protectedOrganizationKey
        };
        CipherResponse cipher = VaultManagerTestData.Cipher(cipherId.ToString(), cipherKey) with
        {
            OrganizationId = organizationId,
            ProtectedCipherKey = protectedCipherKey,
            Attachments = [VaultParserTestData.CreateAttachment(attachmentId.ToString(), "encrypted.txt", plaintext.Length, cipherKey)]
        };
        context.SeedCache([cipher], organizations: [organization]);
        IAppSessionAccessor session = Substitute.For<IAppSessionAccessor>();
        var state = new AppSessionState.Unlocked(context.Account, Substitute.For<FluentBitwarden.AppHost.Modules.Vault.Contracts.IUnlockedVault>(), accountKeys.Session, Substitute.For<IServerNotificationsClient>());
        session.AppSessionState.Returns(state);
        IAttachmentsApi api = Substitute.For<IAttachmentsApi>();
        ConfigureDownload(api, encryptedFile, protectedAttachmentKey);
        var service = new VaultCipherAttachmentService(session, api, context.VaultReaderRepository);

        await service.DownloadAsync(cipherId, attachmentId, destination.FilePath, CancellationToken.None);

        Assert.Equal(plaintext, File.ReadAllBytes(destination.FilePath));
        await api.Received(1).DownloadToAsync(
            context.AccountContext,
            cipherId,
            attachmentId,
            Arg.Any<AttachmentStreamHandler>(),
            CancellationToken.None);
    }

    [Fact]
    public async Task Download_reports_missing_cipher_key_material()
    {
        using var context = new VaultManagerTestContext();
        using var destination = new TemporaryVaultAttachmentFile();
        byte[] userKey = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        using var accountKeys = new AccountKeySessionTestContext(userKey);
        IAppSessionAccessor session = Substitute.For<IAppSessionAccessor>();
        var state = new AppSessionState.Unlocked(context.Account, Substitute.For<FluentBitwarden.AppHost.Modules.Vault.Contracts.IUnlockedVault>(), accountKeys.Session, Substitute.For<IServerNotificationsClient>());
        session.AppSessionState.Returns(state);
        IAttachmentsApi api = Substitute.For<IAttachmentsApi>();
        ConfigureDownload(
            api,
            VaultCipherAttachmentTestData.EncryptEncFile("payload"u8, Enumerable.Repeat((byte)3, 64).ToArray()),
            EncString.Encrypt(Enumerable.Repeat((byte)4, 64).ToArray(), Enumerable.Repeat((byte)3, 64).ToArray()));
        var service = new VaultCipherAttachmentService(session, api, context.VaultReaderRepository);

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DownloadAsync(
            CipherId.Parse("missing-cipher"),
            AttachmentId.Parse("missing-attachment"),
            destination.FilePath,
            CancellationToken.None));

        Assert.Equal("Cipher key material is missing for cipher 'missing-cipher'.", error.Message);
    }

    [Fact]
    public async Task Download_propagates_api_failure()
    {
        using var context = new VaultManagerTestContext();
        using var destination = new TemporaryVaultAttachmentFile();
        byte[] userKey = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        using var accountKeys = new AccountKeySessionTestContext(userKey);
        IAppSessionAccessor session = Substitute.For<IAppSessionAccessor>();
        var state = new AppSessionState.Unlocked(context.Account, Substitute.For<FluentBitwarden.AppHost.Modules.Vault.Contracts.IUnlockedVault>(), accountKeys.Session, Substitute.For<IServerNotificationsClient>());
        session.AppSessionState.Returns(state);
        IAttachmentsApi api = Substitute.For<IAttachmentsApi>();
        api.DownloadToAsync(
                Arg.Any<BitwardenAccountContext>(),
                Arg.Any<CipherId>(),
                Arg.Any<AttachmentId>(),
                Arg.Any<AttachmentStreamHandler>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Attachment request failed.")));
        var service = new VaultCipherAttachmentService(session, api, context.VaultReaderRepository);

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DownloadAsync(
            CipherId.Parse("failed-cipher"),
            AttachmentId.Parse("failed-attachment"),
            destination.FilePath,
            CancellationToken.None));

        Assert.Equal("Attachment request failed.", error.Message);
    }

    [Fact]
    public async Task Download_propagates_cancellation_during_decryption()
    {
        using var context = new VaultManagerTestContext();
        using var destination = new TemporaryVaultAttachmentFile();
        byte[] userKey = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        using var accountKeys = new AccountKeySessionTestContext(userKey);
        CipherId cipherId = CipherId.Parse("cancelled-cipher");
        AttachmentId attachmentId = AttachmentId.Parse("cancelled-attachment");
        byte[] cipherKey = Enumerable.Repeat((byte)2, 64).ToArray();
        byte[] attachmentKey = Enumerable.Repeat((byte)3, 64).ToArray();
        CipherResponse cipher = VaultManagerTestData.Cipher(cipherId.ToString(), cipherKey) with
        {
            ProtectedCipherKey = EncString.Encrypt(cipherKey, userKey)
        };
        context.SeedCache([cipher]);
        IAttachmentsApi api = Substitute.For<IAttachmentsApi>();
        ConfigureDownload(
            api,
            VaultCipherAttachmentTestData.EncryptEncFile("payload"u8, attachmentKey),
            EncString.Encrypt(attachmentKey, cipherKey));
        IAppSessionAccessor session = Substitute.For<IAppSessionAccessor>();
        var state = new AppSessionState.Unlocked(context.Account, Substitute.For<FluentBitwarden.AppHost.Modules.Vault.Contracts.IUnlockedVault>(), accountKeys.Session, Substitute.For<IServerNotificationsClient>());
        session.AppSessionState.Returns(state);
        var service = new VaultCipherAttachmentService(session, api, context.VaultReaderRepository);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.DownloadAsync(
            cipherId,
            attachmentId,
            destination.FilePath,
            cancellation.Token));
    }

    [Fact]
    public async Task Download_rejects_invalid_encrypted_file_data()
    {
        using var context = new VaultManagerTestContext();
        using var destination = new TemporaryVaultAttachmentFile();
        byte[] userKey = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        using var accountKeys = new AccountKeySessionTestContext(userKey);
        CipherId cipherId = CipherId.Parse("invalid-file-cipher");
        AttachmentId attachmentId = AttachmentId.Parse("invalid-file-attachment");
        byte[] cipherKey = Enumerable.Repeat((byte)2, 64).ToArray();
        byte[] attachmentKey = Enumerable.Repeat((byte)3, 64).ToArray();
        CipherResponse cipher = VaultManagerTestData.Cipher(cipherId.ToString(), cipherKey) with
        {
            ProtectedCipherKey = EncString.Encrypt(cipherKey, userKey)
        };
        context.SeedCache([cipher]);
        byte[] encryptedFile = VaultCipherAttachmentTestData.EncryptEncFile("payload"u8, attachmentKey);
        encryptedFile[^1] ^= 1;
        IAttachmentsApi api = Substitute.For<IAttachmentsApi>();
        ConfigureDownload(api, encryptedFile, EncString.Encrypt(attachmentKey, cipherKey));
        IAppSessionAccessor session = Substitute.For<IAppSessionAccessor>();
        var state = new AppSessionState.Unlocked(context.Account, Substitute.For<FluentBitwarden.AppHost.Modules.Vault.Contracts.IUnlockedVault>(), accountKeys.Session, Substitute.For<IServerNotificationsClient>());
        session.AppSessionState.Returns(state);
        var service = new VaultCipherAttachmentService(session, api, context.VaultReaderRepository);

        await Assert.ThrowsAsync<CryptographicException>(() => service.DownloadAsync(
            cipherId,
            attachmentId,
            destination.FilePath,
            CancellationToken.None));
    }

    private void ConfigureDownload(
        IAttachmentsApi api,
        byte[] encryptedFile,
        EncString protectedAttachmentKey)
    {
        api.DownloadToAsync(
                Arg.Any<BitwardenAccountContext>(),
                Arg.Any<CipherId>(),
                Arg.Any<AttachmentId>(),
                Arg.Any<AttachmentStreamHandler>(),
                Arg.Any<CancellationToken>())
            .Returns(call => InvokeDownloadAsync(
                call.Arg<AttachmentStreamHandler>(),
                encryptedFile,
                protectedAttachmentKey));
    }

    private async Task InvokeDownloadAsync(
        AttachmentStreamHandler handler,
        byte[] encryptedFile,
        EncString protectedAttachmentKey)
    {
        using var encryptedStream = new MemoryStream(encryptedFile, writable: false);
        await handler(encryptedStream, protectedAttachmentKey);
    }
}
