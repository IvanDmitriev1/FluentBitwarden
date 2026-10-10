using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Ipc;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Platform.Ipc;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

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
        var cipherHandler = new VaultCipherIpcHandler(sessionService, vaultService);
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
    public async Task Unimplemented_download_operation_remains_unimplemented_when_unlocked()
    {
        IAppSessionService sessionService = Substitute.For<IAppSessionService>();
        IUnlockedSessionLease lease = Substitute.For<IUnlockedSessionLease>();
        sessionService.RequireUnlockedSessionLease().Returns(lease);
        var handler = new VaultCipherIpcHandler(sessionService, Substitute.For<IVaultService>());
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<NotImplementedException>(async () =>
            await handler.DownloadCipherAttachmentAsync(default!, cancellationToken));

        sessionService.Received(1).RequireUnlockedSessionLease();
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
