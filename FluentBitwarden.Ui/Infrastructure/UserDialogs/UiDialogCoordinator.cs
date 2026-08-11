using CommunityToolkit.WinUI;
using FluentBitwarden.Infrastructure.Window;

namespace FluentBitwarden.Infrastructure.UserDialogs;

internal sealed class UiDialogCoordinator(IWindowManager windowManager) : IUiDialogCoordinator
{
    private readonly SemaphoreSlim _presentationGate = new(1, 1);

    public Task<ContentDialogResult> ShowAsync(
        ContentDialog dialog,
        CancellationToken cancellationToken = default) =>
        App.Current.DispatcherQueue.EnqueueAsync(() => ShowCoreAsync(dialog, cancellationToken));

    public Task<TResult> ShowAsync<TResult>(
        IUserDialog<TResult> dialog,
        CancellationToken cancellationToken = default) =>
        App.Current.DispatcherQueue.EnqueueAsync(() => ShowTypedAsync(dialog, cancellationToken));

    private async Task<TResult> ShowTypedAsync<TResult>(IUserDialog<TResult> dialog, CancellationToken cancellationToken)
    {
        if (dialog is not ContentDialog contentDialog)
            throw new ArgumentException($"A user dialog must derive from {nameof(ContentDialog)}.",nameof(dialog));

        await ShowCoreAsync(contentDialog, cancellationToken);
        return dialog.Result;
    }

    private async Task<ContentDialogResult> ShowCoreAsync(ContentDialog contentDialog, CancellationToken cancellationToken)
    {
        await _presentationGate.WaitAsync(cancellationToken);
        WindowMode host = windowManager.ActiveMode;
        try
        {
            contentDialog.XamlRoot = windowManager.XamlRoot;
            using var cancellationRegistration = cancellationToken.Register(static state => HideDialog((ContentDialog)state!), contentDialog);

            return await contentDialog.ShowAsync().AsTask();
        }
        finally
        {
            if (host == WindowMode.Overlay)
            {
                windowManager.CloseWindow();
            }

            _presentationGate.Release();
        }

        static void HideDialog(ContentDialog dialog)
        {
            _ = dialog.DispatcherQueue.TryEnqueue(dialog.Hide);
        }
    }
 
}
