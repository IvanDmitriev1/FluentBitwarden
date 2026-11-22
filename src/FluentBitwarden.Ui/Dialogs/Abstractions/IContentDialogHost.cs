namespace FluentBitwarden.Ui.Dialogs.Abstractions;

public interface IContentDialogHost
{
    Task ShowDialogAsync(
        ContentDialog dialog,
        CancellationToken cancellationToken);
}
