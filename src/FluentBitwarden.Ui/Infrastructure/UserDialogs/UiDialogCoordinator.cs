using CommunityToolkit.WinUI;
using FluentBitwarden.Infrastructure.UserDialogs.Abstractions;
using FluentBitwarden.Infrastructure.Window;

namespace FluentBitwarden.Infrastructure.UserDialogs;

internal sealed class UiDialogCoordinator(IWindowManager windowManager) : IUiDialogCoordinator
{
    private readonly SemaphoreSlim _presentationGate = new(1, 1);

    public Task<ContentDialogResult> ShowAsync(
        Func<ContentDialog> dialogFactory,
        CancellationToken cancellationToken = default) =>
        App.Current.DispatcherQueue.EnqueueAsync(() => ShowCoreAsync(dialogFactory(), cancellationToken));

    public Task<TResult> ShowAsync<TResult>(
        Func<IUserDialog<TResult>> dialogFactory,
        CancellationToken cancellationToken = default) =>
        App.Current.DispatcherQueue.EnqueueAsync(() => ShowTypedAsync(dialogFactory(), cancellationToken));

    private async Task<TResult> ShowTypedAsync<TResult>(IUserDialog<TResult> dialog, CancellationToken cancellationToken)
    {
        if (dialog is not ContentDialog contentDialog)
            throw new ArgumentException($"A user dialog must derive from {nameof(ContentDialog)}.",nameof(dialog));

        await ShowCoreAsync(contentDialog, cancellationToken);
        return dialog.TryGetResult(out var result)
            ? result
            : throw new OperationCanceledException();
    }

    private async Task<ContentDialogResult> ShowCoreAsync(ContentDialog contentDialog, CancellationToken cancellationToken)
    {
        await _presentationGate.WaitAsync(cancellationToken);

        WindowMode host = windowManager.ActiveMode;
        ContentDialogPlacement dialogPlacement = host == WindowMode.Main
            ? ContentDialogPlacement.Popup
            : ContentDialogPlacement.InPlace;

        try
        {
            contentDialog.XamlRoot = windowManager.XamlRoot;
            using var cancellationRegistration = cancellationToken.Register(static state => HideDialog((ContentDialog)state!), contentDialog);

            return await contentDialog.ShowAsync(dialogPlacement).AsTask(cancellationToken);
        }
        finally
        {
            if (host == WindowMode.Main)
                windowManager.MinimizeWindow();
            else
                windowManager.CloseWindow();

            _presentationGate.Release();
        }

        static void HideDialog(ContentDialog dialog)
        {
            _ = dialog.DispatcherQueue.TryEnqueue(dialog.Hide);
        }
    }
 
}
