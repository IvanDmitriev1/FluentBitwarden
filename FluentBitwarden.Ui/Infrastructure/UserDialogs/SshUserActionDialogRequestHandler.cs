using FluentBitwarden.Contracts.Modules.Ssh;
using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Views.UserDialogs;

namespace FluentBitwarden.Infrastructure.UserDialogs;

internal sealed class SshUserActionDialogRequestHandler(
    IUiDialogCoordinator dialogCoordinator) : ISshUserActionDialogClient, IIpcRequestsHandler
{
    public async ValueTask<UserActionDialogOutcome> ShowSshDialogAsync(SshUserActionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dialogCoordinator.ShowAsync<UserActionDialogOutcome>(new SshUserActionDialog(request), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return UserActionDialogOutcome.Denied;
        }
    }
}
