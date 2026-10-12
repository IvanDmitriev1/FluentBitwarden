using FluentBitwarden.AppHost.Infrastructure.Processes;
using FluentBitwarden.AppHost.Integrations.SshAgent.Services;
using FluentBitwarden.Contracts.Integrations.Ssh;
using FluentBitwarden.Contracts.Modules.Ssh;
using FluentBitwarden.Platform.Ipc.Abstractions;
using NSubstitute;

namespace FluentBitwarden.AppHost.IntegrationTests.Integrations.SshAgent;

public sealed class SshUserActionDialogClientTests
{
    [Fact]
    public async Task ShowSshDialog_returns_the_ipc_decision_and_activates_the_ui()
    {
        IIpcClient ipcClient = Substitute.For<IIpcClient>();
        IUiProcessLauncher uiProcessLauncher = Substitute.For<IUiProcessLauncher>();
        var request = new SshUserActionRequest("work key", "SHA256:test", false);
        ipcClient.SendAsync<SshUserActionRequest, UserActionDialogOutcome>(
                request,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(UserActionDialogOutcome.Approved));
        var client = new SshUserActionDialogClient(ipcClient, uiProcessLauncher);

        UserActionDialogOutcome outcome = await client.ShowSshDialogAsync(request, CancellationToken.None);

        Assert.Equal(UserActionDialogOutcome.Approved, outcome);
        _ = ipcClient.Received(1).SendAsync<SshUserActionRequest, UserActionDialogOutcome>(
            request,
            Arg.Any<CancellationToken>());
        uiProcessLauncher.Received(1).Activate();
    }

    [Fact]
    public async Task ShowSshDialog_denies_when_ipc_fails()
    {
        IIpcClient ipcClient = Substitute.For<IIpcClient>();
        IUiProcessLauncher uiProcessLauncher = Substitute.For<IUiProcessLauncher>();
        var request = new SshUserActionRequest("work key", "SHA256:test", false);
        ipcClient.SendAsync<SshUserActionRequest, UserActionDialogOutcome>(
                request,
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<UserActionDialogOutcome>(new IOException("IPC unavailable")));
        var client = new SshUserActionDialogClient(ipcClient, uiProcessLauncher);

        UserActionDialogOutcome outcome = await client.ShowSshDialogAsync(request, CancellationToken.None);

        Assert.Equal(UserActionDialogOutcome.Denied, outcome);
        uiProcessLauncher.Received(1).Activate();
    }
}
