using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.AppHost.Infrastructure.Processes;
using FluentBitwarden.Contracts.Integrations.Ssh;
using FluentBitwarden.Contracts.Modules.Ssh;
using FluentBitwarden.Platform.Ipc.Abstractions;

namespace FluentBitwarden.AppHost.Integrations.SshAgent.Services;

internal class SshUserActionDialogClient(
    IIpcClient ipcClient,
    IUiProcessLauncher uiProcessLauncher) : ISshUserActionDialogClient
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Any IPC failure while asking the user for an SSH action decision must be treated as a denial, not crash the agent.")]
    public async Task<UserActionDialogOutcome> ShowSshDialogAsync(
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
