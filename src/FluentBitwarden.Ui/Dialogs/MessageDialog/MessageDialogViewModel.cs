using CommunityToolkit.Mvvm.Input;

namespace FluentBitwarden.Ui.Dialogs;

public sealed partial class MessageDialogViewModel(string title, string message)
{
    public string Title { get; } = title;
    public string Message { get; } = message;
    public MessageDialogResult Result { get; private set; } = MessageDialogResult.Dismissed;

    [RelayCommand]
    private void PrimaryButton(ContentDialogButtonClickEventArgs args) => Result = MessageDialogResult.Acknowledged;
}
