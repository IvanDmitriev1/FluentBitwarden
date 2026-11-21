namespace FluentBitwarden.Ui.Dialogs.Abstractions;

public interface IContentDialogHost
{
    Task<ContentDialogResult> ShowDialogAsync(
        ContentDialog dialog,
        CancellationToken cancellationToken);
}
