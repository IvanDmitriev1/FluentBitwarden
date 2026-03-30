using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Microsoft.UI.Xaml.Navigation;

namespace FluentBitwarden.Shared.Behaviors.Lifecycle;

public abstract class LifecyclePage : Page
{

    private CancellationTokenSource? _cts;

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        CancelCts();
        _cts = new CancellationTokenSource();

        try
        {
            Task task = Task.CompletedTask;

            if (DataContext is IPageLifecycleAware lifecycleAware)
            {
                task = lifecycleAware.OnLoadingAsync(_cts.Token);
            }

            if (DataContext is IPageLifecycleAwareParam lifecycleAwareParam)
            {
                task = lifecycleAwareParam.OnLoadingAsync(e.Parameter, _cts.Token);
            }

            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (_cts.IsCancellationRequested)
        {
            Debug.WriteLine($"Canceled page loading for {GetType().Name}: {ex.Message}");
        }
        catch (Exception ex)
        {
            DispatcherQueue.TryEnqueue(() => ExceptionDispatchInfo.Throw(ex));
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        CancelCts();

        if (DataContext is IPageLifecycleAware lifecycleAware)
        {
            lifecycleAware.OnUnloading();
        }
    }

    private void CancelCts()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
