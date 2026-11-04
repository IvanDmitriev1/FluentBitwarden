using FluentBitwarden.AppHost.Infrastructure;
using FluentBitwarden.AppHost.Infrastructure.Processes;
using FluentBitwarden.Contracts.Infrastructure.UserDialog;
using FluentBitwarden.Contracts.Integrations.Ssh;
using FluentBitwarden.Contracts.Modules.Ssh;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.IntegrationTests.Infrastructure;

public sealed class UserDialogServiceTests
{
    [Fact]
    public async Task ShowAsync_forwards_request_and_token_and_activates_ui()
    {
        IIpcClient ipc = Substitute.For<IIpcClient>();
        IUiProcessLauncher launcher = Substitute.For<IUiProcessLauncher>();
        var request = new SshUserActionRequest("key", "fingerprint", false);
        using var cancellation = new CancellationTokenSource();
        var expected = UserDialogResult<SshUserActionResponse>.Completed(new(true));
        ipc.SendAsync<SshUserActionRequest, UserDialogResult<SshUserActionResponse>>(request, cancellation.Token)
            .Returns(expected);
        var service = new UserDialogService(ipc, launcher);

        var actual = await service.ShowAsync<SshUserActionResponse, SshUserActionRequest>(request, cancellation.Token);

        Assert.Equal(expected, actual);
        launcher.Received(1).Activate();
        _ = ipc.Received(1).SendAsync<SshUserActionRequest, UserDialogResult<SshUserActionResponse>>(request, cancellation.Token);
    }

    [Fact]
    public async Task ShowAsync_maps_ipc_cancellation_to_cancelled_result()
    {
        IIpcClient ipc = Substitute.For<IIpcClient>();
        ipc.SendAsync<SshUserActionRequest, UserDialogResult<SshUserActionResponse>>(
                Arg.Any<SshUserActionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<UserDialogResult<SshUserActionResponse>>(new OperationCanceledException()));
        var service = new UserDialogService(ipc, Substitute.For<IUiProcessLauncher>());

        var result = await service.ShowAsync<SshUserActionResponse, SshUserActionRequest>(
            new("key", "fingerprint", false), TestContext.Current.CancellationToken);

        Assert.False(result.TryGetResult(out _));
    }

    [Fact]
    public async Task ShowAsync_serializes_overlapping_dialogs()
    {
        IIpcClient ipc = Substitute.For<IIpcClient>();
        var firstResponse = new TaskCompletionSource<UserDialogResult<SshUserActionResponse>>(TaskCreationOptions.RunContinuationsAsynchronously);
        int sendCount = 0;
        ipc.SendAsync<SshUserActionRequest, UserDialogResult<SshUserActionResponse>>(
                Arg.Any<SshUserActionRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Interlocked.Increment(ref sendCount) == 1
                ? firstResponse.Task
                : Task.FromResult(UserDialogResult<SshUserActionResponse>.Completed(new(true))));
        IUiProcessLauncher launcher = Substitute.For<IUiProcessLauncher>();
        var service = new UserDialogService(ipc, launcher);
        var request = new SshUserActionRequest("key", "fingerprint", false);

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Task<UserDialogResult<SshUserActionResponse>> first = service.ShowAsync<SshUserActionResponse, SshUserActionRequest>(request, cancellationToken);
        Assert.Equal(1, Volatile.Read(ref sendCount));

        Task<UserDialogResult<SshUserActionResponse>> second = service.ShowAsync<SshUserActionResponse, SshUserActionRequest>(request, cancellationToken);

        Assert.False(second.IsCompleted);
        Assert.Equal(1, Volatile.Read(ref sendCount));
        launcher.Received(1).Activate();

        firstResponse.SetResult(UserDialogResult<SshUserActionResponse>.Completed(new(true)));
        Assert.True((await first).TryGetResult(out _));
        Assert.True((await second).TryGetResult(out _));
        Assert.Equal(2, Volatile.Read(ref sendCount));
        launcher.Received(2).Activate();
    }

    [Fact]
    public async Task ShowAsync_releases_gate_after_ipc_failure()
    {
        IIpcClient ipc = Substitute.For<IIpcClient>();
        int sendCount = 0;
        ipc.SendAsync<SshUserActionRequest, UserDialogResult<SshUserActionResponse>>(
                Arg.Any<SshUserActionRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Interlocked.Increment(ref sendCount) == 1
                ? Task.FromException<UserDialogResult<SshUserActionResponse>>(new IOException("IPC unavailable"))
                : Task.FromResult(UserDialogResult<SshUserActionResponse>.Completed(new(true))));
        var service = new UserDialogService(ipc, Substitute.For<IUiProcessLauncher>());
        var request = new SshUserActionRequest("key", "fingerprint", false);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<IOException>(() => service.ShowAsync<SshUserActionResponse, SshUserActionRequest>(request, cancellationToken));
        UserDialogResult<SshUserActionResponse> subsequent = await service.ShowAsync<SshUserActionResponse, SshUserActionRequest>(request, cancellationToken);

        Assert.True(subsequent.TryGetResult(out _));
        Assert.Equal(2, Volatile.Read(ref sendCount));
    }
}
