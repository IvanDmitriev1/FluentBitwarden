using FluentBitwarden.Contracts.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Contracts.Infrastructure.UserDialog;
using FluentBitwarden.Contracts.Modules.Ssh;
using FluentBitwarden.Infrastructure.Abstractions.Dialog;

namespace FluentBitwarden.Infrastructure.IpcClientsImplementations;

internal sealed class UserDialogRequestHandler(IContentDialogService contentDialogService)
    : IUserDialogClient, IIpcRequestsHandler
{
    private static readonly ContentDialogOptions SshDialogOptions = new(
        Title: "Approve SSH request?",
        PrimaryButtonText: "Approve",
        SecondaryButtonText: "Deny",
        DefaultButton: ContentDialogButton.Secondary,
        DataTemplateKey: "SshUserActionRequestViewModelTemplateKey");

    public async ValueTask<UserActionDialogOutcome> ShowSshDialogAsync(
        SshUserActionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await contentDialogService.ShowUserActionAsync(
            request,
            SshDialogOptions);

        return result;
    }
}
