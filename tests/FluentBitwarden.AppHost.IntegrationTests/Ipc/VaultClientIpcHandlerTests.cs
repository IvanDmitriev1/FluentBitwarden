using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Ipc;
using FluentBitwarden.AppHost.Modules.Vault.Contracts;
using FluentBitwarden.Contracts.AppSession;
using NSubstitute;

namespace FluentBitwarden.AppHost.IntegrationTests.Ipc;

public sealed class VaultClientIpcHandlerTests
{
    [Fact]
    public async Task Every_vault_operation_requires_an_unlocked_session_lease()
    {
        IAppSessionService sessionService = Substitute.For<IAppSessionService>();
        sessionService.RequireUnlockedSessionLease().Returns(_ => throw new UnlockedSessionRequiredException());
        var handler = new VaultClientIpcHandler(sessionService, Substitute.For<IVaultManager>());
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<UnlockedSessionRequiredException>(() =>
            handler.SyncAsync(default!, cancellationToken));
        await Assert.ThrowsAsync<UnlockedSessionRequiredException>(async () =>
            await handler.GetFoldersAsync(default!, cancellationToken));
        await Assert.ThrowsAsync<UnlockedSessionRequiredException>(async () =>
            await handler.SearchCiphersAsync(default!, cancellationToken));
        await Assert.ThrowsAsync<UnlockedSessionRequiredException>(async () =>
            await handler.GetCipherAsync(default!, cancellationToken));
        await Assert.ThrowsAsync<UnlockedSessionRequiredException>(async () =>
            await handler.SaveCipherAsync(default!, cancellationToken));
        await Assert.ThrowsAsync<UnlockedSessionRequiredException>(async () =>
            await handler.DownloadCipherAttachmentAsync(default!, cancellationToken));

        sessionService.Received(6).RequireUnlockedSessionLease();
    }

    [Fact]
    public async Task Unimplemented_download_operation_remains_unimplemented_when_unlocked()
    {
        IAppSessionService sessionService = Substitute.For<IAppSessionService>();
        IUnlockedSessionLease lease = Substitute.For<IUnlockedSessionLease>();
        sessionService.RequireUnlockedSessionLease().Returns(lease);
        var handler = new VaultClientIpcHandler(sessionService, Substitute.For<IVaultManager>());
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<NotImplementedException>(async () =>
            await handler.DownloadCipherAttachmentAsync(default!, cancellationToken));

        sessionService.Received(1).RequireUnlockedSessionLease();
    }
}
