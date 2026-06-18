using CommunityToolkit.WinUI;
using FluentBitwarden.Services.Window;
using FluentBitwarden.Views.Shell;
using Microsoft.UI.Dispatching;

namespace FluentBitwarden.Services.UserDialogs;

internal sealed class UiDialogDispatcher(IWindowManager windowManager)
{
    public Task<T> EnqueueAsync<T>(Func<Task<T>> showDialogAsync) =>
        App.Current.DispatcherQueue.EnqueueAsync(async () =>
        {
            try
            {
                return await showDialogAsync();
            }
            finally
            {
                if (windowManager.ActiveWindow is OverlayWindow)
                {
                    windowManager.CloseWindow();
                }
            }
        }, DispatcherQueuePriority.High);
}
