using FluentBitwarden.Ui.Dialogs.Abstractions;

namespace FluentBitwarden.Ui.Dialogs;

internal sealed class DialogService(IContentDialogHost contentDialogHost) : IDialogService
{
    
    public async Task<TResult> ShowAsync<T, TArgs, TResult>(TArgs args, CancellationToken cancellationToken = default)
        where T : ContentDialog, IContentDialog<T, TArgs, TResult>
        where TResult : notnull
    {
        T dialog = T.Create(args);
        await contentDialogHost.ShowDialogAsync(dialog, cancellationToken);

        return dialog.Result;
    }
}
