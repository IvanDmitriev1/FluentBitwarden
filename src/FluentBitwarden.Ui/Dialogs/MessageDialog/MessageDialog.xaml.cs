using FluentBitwarden.Ui.Dialogs.Abstractions;

namespace FluentBitwarden.Ui.Dialogs;

public sealed partial class MessageDialog :
    ContentDialog,
    IContentDialog<MessageDialog, MessageDialogArgs, MessageDialogResult>
{
    public static MessageDialog Create(MessageDialogArgs args) => new(new MessageDialogViewModel(args.Title, args.Message));

    public MessageDialog(MessageDialogViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public MessageDialogViewModel ViewModel { get; }
    public MessageDialogResult Result => ViewModel.Result;
}
