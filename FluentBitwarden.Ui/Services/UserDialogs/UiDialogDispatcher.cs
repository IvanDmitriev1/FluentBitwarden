using CommunityToolkit.WinUI;
using FluentBitwarden.Services.Window;
using FluentBitwarden.Views.Shell;
using Microsoft.UI.Dispatching;

namespace FluentBitwarden.Services.UserDialogs;

internal sealed class UiDialogDispatcher(IWindowManager windowManager)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private int _pendingDialogs;

    public async Task<T> EnqueueAsync<T>(Func<Task<T>> showDialogAsync)
    {
        Interlocked.Increment(ref _pendingDialogs);
        return await App.Current.DispatcherQueue.EnqueueAsync(async () =>
        {
            await _gate.WaitAsync();
            object? host = null;
            try
            {
                host = windowManager.ActiveWindow;
                return await showDialogAsync();
            }
            finally
            {
                bool isLastDialog = Interlocked.Decrement(ref _pendingDialogs) == 0;
                _gate.Release();

                if (isLastDialog &&
                    host is OverlayWindow &&
                    windowManager.HasWindow &&
                    ReferenceEquals(windowManager.ActiveWindow, host))
                {
                    windowManager.CloseWindow();
                }
            }
        }, DispatcherQueuePriority.High);
    }
}
