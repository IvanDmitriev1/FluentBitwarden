using FluentBitwarden.AppHost.Infrastructure.Abstractions;
using FluentBitwarden.Contracts.Infrastructure;
using FluentBitwarden.Contracts.Ipc.Abstractions;
using FluentBitwarden.Contracts.Modules.Ssh;

namespace FluentBitwarden.AppHost.Infrastructure.Services;

internal sealed class SshUserActionDialogClient(
    IIpcClient ipcClient,
    IUiProcessLauncher uiProcessLauncher) : ISshUserActionDialogClient
{
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
