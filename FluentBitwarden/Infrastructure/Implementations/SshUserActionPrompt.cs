using FluentBitwarden.Infrastructure.Abstractions.Dialog;
using FluentBitwarden.Resources.Dialogs.Models;

namespace FluentBitwarden.Infrastructure.Implementations;

/*
internal sealed class SshUserActionPrompt(IContentDialogService contentDialogService) : ISshUserActionPrompt
{
    private static readonly ContentDialogOptions DialogOptions = new(
        Title: "Approve SSH request?",
        PrimaryButtonText: "Approve",
        SecondaryButtonText: "Deny",
        DefaultButton: ContentDialogButton.Secondary,
        DataTemplateKey: "SshUserActionRequestViewModelTemplateKey");

    public Task<UserActionDialogOutcome> PromptAsync(SshUserActionRequestViewModel request) =>
        contentDialogService.ShowUserActionAsync(
            new SshUserActionDialogViewModel(request.KeyName, request.KeyFingerprint, request.IsForwarded),
            DialogOptions);
}
*/
