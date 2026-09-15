using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.AppHost.Infrastructure.Services;
using FluentBitwarden.Contracts.Modules.Ssh;

namespace FluentBitwarden.AppHost.Modules.SshAgent.Ipc;

internal sealed class SshUserActionDialogClient(
    IIpcClient ipcClient,
    IUiProcessLauncher uiProcessLauncher) : ISshUserActionDialogClient
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Any IPC failure while asking the user for an SSH action decision must be treated as a denial, not crash the agent.")]
    public async ValueTask<UserActionDialogOutcome> ShowSshDialogAsync(
        SshUserActionRequest request,
        CancellationToken cancellationToken = default)
    {
        uiProcessLauncher.Activate();

        try
        {
            return await ipcClient.SendAsync<SshUserActionRequest, UserActionDialogOutcome>(
                request,
                cancellationToken);
        }
        catch (Exception)
        {
            return UserActionDialogOutcome.Denied;
        }
    }
}
