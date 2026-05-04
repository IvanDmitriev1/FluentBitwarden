using FluentBitwarden.Resources.Dialogs.Models;
using FluentBitwarden.Shared.Services.Abstractions.Dialog;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Resources.Dialogs;

public sealed partial class UserActionContentDialog : ContentDialog
{
    public UserActionContentDialog(IContentDialogViewModel viewModel, DataTemplate contentTemplate)
    {
        Content = viewModel;
        ContentTemplate = contentTemplate;

        Title = viewModel.DialogTitle;

        DefaultButton = ContentDialogButton.Secondary;
        PrimaryButtonText = "Approve";
        SecondaryButtonText = "Deny";

        PrimaryButtonClick += (_, _) => _cts.TrySetResult(UserActionDialogOutcome.Approved);
        CloseButtonClick += (_, _) => _cts.TrySetResult(UserActionDialogOutcome.Denied);
        Closed += (_, _) => _cts.TrySetResult(UserActionDialogOutcome.Denied);
    }

    private readonly TaskCompletionSource<UserActionDialogOutcome> _cts =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public new async Task<UserActionDialogOutcome> ShowAsync()
    {
        await base.ShowAsync();
        return await _cts.Task;
    }
}