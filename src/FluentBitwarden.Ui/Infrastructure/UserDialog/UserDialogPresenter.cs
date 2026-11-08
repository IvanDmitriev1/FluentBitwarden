using CommunityToolkit.WinUI;
using FluentBitwarden.Contracts.Infrastructure.UserDialog;
using FluentBitwarden.Ui.Infrastructure.UserDialog.Abstractions;
using FluentBitwarden.Ui.Infrastructure.Windowing.Abstractions;
using FluentBitwarden.Ui.Windows;
using Microsoft.UI.Xaml.Controls;
using WinUIEx;

namespace FluentBitwarden.Ui.Infrastructure.UserDialog;

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

        bool mainWindow = windowManager.ActiveWindow is MainWindow;
        ContentDialogPlacement dialogPlacement = windowManager.ActiveWindow is MainWindow
            ? ContentDialogPlacement.Popup
            : ContentDialogPlacement.InPlace;

        try
        {
            var dialogPresentable = windowManager.ActiveWindow as IDialogHost;
            ArgumentNullException.ThrowIfNull(dialogPresentable);

            contentDialog.XamlRoot = dialogPresentable.XamlRoot;
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
            if (mainWindow)
                windowManager.ActiveWindow.Minimize();
            else
                windowManager.ActiveWindow.Close();

            _presentationGate.Release();
        }

        static void HideDialog(ContentDialog dialog)
        {
            _ = dialog.DispatcherQueue.TryEnqueue(dialog.Hide);
        }
    }
}
