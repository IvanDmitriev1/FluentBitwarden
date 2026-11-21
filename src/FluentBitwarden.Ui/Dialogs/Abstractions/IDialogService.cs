namespace FluentBitwarden.Ui.Dialogs.Abstractions;

public interface IDialogService
{
    Task<TResult> ShowAsync<T, TArgs, TResult>(
        TArgs args,
        CancellationToken cancellationToken = default)
        where T : ContentDialog, IContentDialog<T, TArgs, TResult>
        where TResult : notnull;
}
