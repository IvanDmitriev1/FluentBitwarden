using CommunityToolkit.WinUI;
using FluentBitwarden.Contracts.Infrastructure.UserDialog;
using FluentBitwarden.Infrastructure.UserDialogs.Abstractions;
using FluentBitwarden.Infrastructure.Window;

namespace FluentBitwarden.Infrastructure.UserDialogs;

internal sealed class UserDialogPresenter(IWindowManager windowManager) : IUserDialogPresenter
{
    private readonly SemaphoreSlim _presentationGate = new(1, 1);

    public Task<UserDialogResult<TResult>> ShowAsync<TResult>(Func<IUserDialog<TResult>> dialogFactory, CancellationToken cancellationToken = default)
        where TResult : notnull
        => App.Current.DispatcherQueue.EnqueueAsync(() => ShowCoreAsync(dialogFactory, cancellationToken));

    private async Task<UserDialogResult<TResult>> ShowCoreAsync<TResult>(Func<IUserDialog<TResult>> dialogFactory, CancellationToken cancellationToken)
        where TResult : notnull
    {
        var dialog = dialogFactory.Invoke();

        if (dialog is not ContentDialog contentDialog)
            throw new ArgumentException($"A user dialog must derive from {nameof(ContentDialog)}.", nameof(dialog));

        await ShowDialogAsync(contentDialog, cancellationToken);

        return dialog.TryGetResult(out var result)
            ? UserDialogResult<TResult>.Completed(result)
            : UserDialogResult<TResult>.Cancelled;
    }

    private async Task ShowDialogAsync(ContentDialog contentDialog, CancellationToken cancellationToken)
    {
        await _presentationGate.WaitAsync(cancellationToken);

        WindowMode host = windowManager.ActiveMode;
        ContentDialogPlacement dialogPlacement = host == WindowMode.Main
            ? ContentDialogPlacement.Popup
            : ContentDialogPlacement.InPlace;

        try
        {
            contentDialog.XamlRoot = windowManager.XamlRoot;
            using var cancellationRegistration =
                cancellationToken.Register(static state => HideDialog((ContentDialog)state!), contentDialog);

            await contentDialog.ShowAsync(dialogPlacement).AsTask(cancellationToken);
            return;
        }
        catch (OperationCanceledException)
        {
            
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
