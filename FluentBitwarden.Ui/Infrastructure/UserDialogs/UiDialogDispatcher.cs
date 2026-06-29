using CommunityToolkit.WinUI;
using FluentBitwarden.Infrastructure.Window;

namespace FluentBitwarden.Infrastructure.UserDialogs;

internal sealed class UiDialogDispatcher(IWindowManager windowManager)
{
    public Task<T> EnqueueAsync<T>(Func<Task<T>> showDialogAsync) =>
        App.Current.DispatcherQueue.EnqueueAsync(async () =>
        {
            WindowMode host = windowManager.ActiveMode;

            try
            {
                return await showDialogAsync.Invoke();
            }
            finally
            {
                if (host == WindowMode.Overlay)
                {
                    windowManager.CloseWindow();
                }
            }
        });
}
