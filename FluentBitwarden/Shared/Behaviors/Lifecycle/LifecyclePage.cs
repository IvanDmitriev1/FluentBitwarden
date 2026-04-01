using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;

namespace FluentBitwarden.Shared.Behaviors.Lifecycle;

public abstract class LifecyclePage : Page
{
    private CancellationTokenSource? _cts;
    private RoutedEventHandler? _loadedHandler;
    private long _loadVersion;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        CancelPendingLoad();
        _cts = new CancellationTokenSource();

        var loadVersion = ++_loadVersion;
        ScheduleLoading(e.Parameter, loadVersion);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        CancelPendingLoad();
        ++_loadVersion;

        (DataContext as IPageLifecycleAwareBase)?.OnUnloading();
    }

    private void ScheduleLoading(object? parameter, long loadVersion)
    {
        if (IsLoaded)
        {
            EnqueueLoading(parameter, loadVersion);
            return;
        }

        RoutedEventHandler? handler = null;
        handler = (_, _) =>
        {
            Loaded -= handler;

            if (ReferenceEquals(_loadedHandler, handler))
            {
                _loadedHandler = null;
            }

            EnqueueLoading(parameter, loadVersion);
        };

        _loadedHandler = handler;
        Loaded += handler;
    }

    private void EnqueueLoading(object? parameter, long loadVersion)
    {
        if (!IsCurrentLoad(loadVersion))
            return;

        if (DispatcherQueue.TryEnqueue(() => StartLoading(parameter, loadVersion)))
            return;

        StartLoading(parameter, loadVersion);
    }

    private async void StartLoading(object? parameter, long loadVersion)
    {
        await RunLoadingAsync(parameter, loadVersion);
    }

    private async Task RunLoadingAsync(object? parameter, long loadVersion)
    {
        if (_cts is not { } cts || !IsCurrentLoad(loadVersion, cts))
        {
            return;
        }

        try
        {
            Task loadingTask = (DataContext, parameter) switch
            {
                ({ } target, IPageNavigationParameter navigationParameter) =>
                    navigationParameter.Load(target, cts.Token),

                (IPageLifecycleAware lifecycleAware, _) =>
                    lifecycleAware.OnLoadingAsync(cts.Token),

                _ => Task.CompletedTask
            };

            if (!IsCurrentLoad(loadVersion, cts))
            {
                return;
            }

            await loadingTask;
        }
        catch (OperationCanceledException ex) when (cts.IsCancellationRequested)
        {
            Debug.WriteLine($"Canceled page loading for {GetType().Name}: {ex.Message}");
        }
    }

    private bool IsCurrentLoad(long loadVersion, CancellationTokenSource? cts = null)
    {
        return loadVersion == _loadVersion && (cts is null || ReferenceEquals(_cts, cts));
    }

    private void CancelPendingLoad()
    {
        if (_loadedHandler is not null)
        {
            Loaded -= _loadedHandler;
            _loadedHandler = null;
        }

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
