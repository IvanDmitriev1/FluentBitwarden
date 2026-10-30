using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Ipc;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Attachments;
using FluentBitwarden.Platform.Ipc;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.AppHost.IntegrationTests.Ipc;

public sealed class VaultClientIpcHandlerTests
{
    [Fact]
    public async Task Every_vault_operation_requires_an_unlocked_session_lease()
    {
        IAppSessionAccessor accessor = Substitute.For<IAppSessionAccessor>();
        accessor.AppSessionState.Returns(new AppSessionState.NotAuthenticated());
        using var context = new FluentBitwarden.AppHost.IntegrationTests.Modules.Vault.VaultManagerTestContext();
        context.SessionAccessor.AppSessionState.Returns(new AppSessionState.NotAuthenticated());
        var vaultService = context.VaultService;
        var operationsHandler = new VaultOperationsIpcHandler(vaultService);
        var cipherHandler = new VaultCipherIpcHandler(
            accessor,
            vaultService,
            new FluentBitwarden.AppHost.Modules.Vault.Services.VaultCipherAttachmentService(context.SessionAccessor, Substitute.For<BitwardenApi.Vault.Attachments.IAttachmentsApi>(), context.VaultReaderRepository));
        var folderHandler = new VaultFolderIpcHandler(accessor);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<UnlockedSessionRequiredException>(() =>
            operationsHandler.SyncAsync(default!, cancellationToken));
        await Assert.ThrowsAsync<UnlockedSessionRequiredException>(async () =>
            await folderHandler.GetFoldersAsync(default!, cancellationToken));
        await Assert.ThrowsAsync<UnlockedSessionRequiredException>(async () =>
            await cipherHandler.SearchCiphersAsync(default!, cancellationToken));
        await Assert.ThrowsAsync<UnlockedSessionRequiredException>(async () =>
            await cipherHandler.GetCipherAsync(default!, cancellationToken));
        await Assert.ThrowsAsync<UnlockedSessionRequiredException>(async () =>
            await cipherHandler.SaveCipherAsync(new FluentBitwarden.Contracts.Modules.Vault.Ciphers.SaveVaultCipherRequest(Cipher.CreateBlankCipher(CipherType.Login)), cancellationToken));
        await Assert.ThrowsAsync<UnlockedSessionRequiredException>(async () =>
            await cipherHandler.DownloadCipherAttachmentAsync(default!, cancellationToken));

        _ = accessor.Received(3).AppSessionState;
    }

    [Fact]
    public async Task Download_attachment_delegates_to_service_with_the_unlocked_session()
    {
        IAppSessionAccessor accessor = Substitute.For<IAppSessionAccessor>();
        IVaultCipherAttachmentService attachmentService = Substitute.For<IVaultCipherAttachmentService>();
        var handler = new VaultCipherIpcHandler(
            accessor,
            Substitute.For<IVaultService>(),
            attachmentService);
        var request = new DownloadVaultCipherAttachmentRequest(
            CipherId.Parse("synthetic-cipher"),
            AttachmentId.Parse("synthetic-attachment"),
            Path.Combine("destination", "attachment.bin"));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await handler.DownloadCipherAttachmentAsync(request, cancellationToken);

        await attachmentService.Received(1).DownloadAsync(
            request.CipherId,
            request.AttachmentId,
            request.DestinationPath,
            cancellationToken);
    }

    [Fact]
    public void Actual_vault_handlers_register_without_duplicate_message_ids_and_as_scoped()
    {
        var services = new ServiceCollection();
        var builder = new IpcRpcHandlerBuilder(services);

        builder
            .Add<VaultOperationsIpcHandler>()
            .Add<VaultCipherIpcHandler>()
            .Add<VaultFolderIpcHandler>();

        ServiceDescriptor operations = Assert.Single(
            services,
            static descriptor => descriptor.ServiceType == typeof(VaultOperationsIpcHandler));
        ServiceDescriptor ciphers = Assert.Single(
            services,
            static descriptor => descriptor.ServiceType == typeof(VaultCipherIpcHandler));
        ServiceDescriptor folders = Assert.Single(
            services,
            static descriptor => descriptor.ServiceType == typeof(VaultFolderIpcHandler));

        Assert.Equal(ServiceLifetime.Scoped, operations.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, ciphers.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, folders.Lifetime);
    }
}
