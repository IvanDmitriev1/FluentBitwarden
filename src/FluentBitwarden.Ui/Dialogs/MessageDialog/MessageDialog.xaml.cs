using FluentBitwarden.Ui.Dialogs.Abstractions;
using MessageDialogArgs = (string Title, string Message);

namespace FluentBitwarden.Ui.Dialogs;

public sealed partial class MessageDialog :
    ContentDialog,
    IContentDialog<MessageDialog, MessageDialogArgs, MessageDialogResult>
{
    public static MessageDialog Create(MessageDialogArgs args) =>
        new(args.Title, args.Message);

    public MessageDialog(string title, string message)
    {
        InitializeComponent();

        Title = title;
        MessageText.Text = message;
    }

    public MessageDialogResult Result { get; private set; } = MessageDialogResult.Dismissed;

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Result = MessageDialogResult.Acknowledged;
    }
}
