using FluentBitwarden.Contracts.Infrastructure.UserDialog;
using FluentBitwarden.Contracts.Integrations.Ssh;
using FluentBitwarden.Contracts.Modules.Ssh;
using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Ui.Dialogs.Abstractions;
using FluentBitwarden.Ui.Dialogs.SshKeyApproval;
using FluentBitwarden.Ui.Infrastructure.Hosting;

namespace FluentBitwarden.Ui.Infrastructure.RemoteHandlers;

internal sealed class SshKeyApprovalIpcHandler(
    IDialogService dialogService,
    WinUiHostedService<App> uiThread) : IIpcRequestsHandler
{
    public async Task<UserDialogResult<SshUserActionResponse>> ShowSshDialog(
        SshUserActionRequest request,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return UserDialogResult<SshUserActionResponse>.Cancelled;

        try
        {
            var result = await uiThread.RunOnUiThreadAsync(() =>
                dialogService.ShowAsync<SshKeyApprovalDialog, SshUserActionRequest, SshKeyApprovalResult>(
                    request,
                    cancellationToken));

            return UserDialogResult<SshUserActionResponse>.Completed(
                new SshUserActionResponse(result == SshKeyApprovalResult.Approved));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return UserDialogResult<SshUserActionResponse>.Cancelled;
        }
    }
}
