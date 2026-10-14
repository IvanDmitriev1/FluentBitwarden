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
        IAppSessionService sessionService = Substitute.For<IAppSessionService>();
        sessionService.RequireUnlockedSessionLease().Returns(_ => throw new UnlockedSessionRequiredException());
        var vaultService = Substitute.For<IVaultService>();
        var operationsHandler = new VaultOperationsIpcHandler(sessionService, vaultService);
        var cipherHandler = new VaultCipherIpcHandler(
            sessionService,
            vaultService,
            Substitute.For<IVaultCipherAttachmentService>());
        var folderHandler = new VaultFolderIpcHandler(sessionService);
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
            await cipherHandler.SaveCipherAsync(default!, cancellationToken));
        await Assert.ThrowsAsync<UnlockedSessionRequiredException>(async () =>
            await cipherHandler.DownloadCipherAttachmentAsync(default!, cancellationToken));

        sessionService.Received(6).RequireUnlockedSessionLease();
    }

    [Fact]
    public async Task Download_attachment_delegates_to_service_with_the_unlocked_session()
    {
        IAppSessionService sessionService = Substitute.For<IAppSessionService>();
        IUnlockedSessionLease lease = Substitute.For<IUnlockedSessionLease>();
        sessionService.RequireUnlockedSessionLease().Returns(lease);
        IVaultCipherAttachmentService attachmentService = Substitute.For<IVaultCipherAttachmentService>();
        var handler = new VaultCipherIpcHandler(
            sessionService,
            Substitute.For<IVaultService>(),
            attachmentService);
        var request = new DownloadVaultCipherAttachmentRequest(
            CipherId.Parse("synthetic-cipher"),
            AttachmentId.Parse("synthetic-attachment"),
            Path.Combine("destination", "attachment.bin"));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await handler.DownloadCipherAttachmentAsync(request, cancellationToken);

        sessionService.Received(1).RequireUnlockedSessionLease();
        await attachmentService.Received(1).DownloadAsync(
            lease,
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
