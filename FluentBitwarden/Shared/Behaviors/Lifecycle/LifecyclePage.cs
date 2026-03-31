using System.Diagnostics;
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
            Task loadingTask = (DataContext, e.Parameter) switch
            {
                ({ } target, IPageNavigationParameter parameter) =>
                    parameter.Load(target, _cts.Token),

                (IPageLifecycleAware lifecycleAware, _) =>
                    lifecycleAware.OnLoadingAsync(_cts.Token),

                _ => Task.CompletedTask
            };

            await loadingTask;
        }
        catch (OperationCanceledException ex) when (_cts.IsCancellationRequested)
        {
            Debug.WriteLine($"Canceled page loading for {GetType().Name}: {ex.Message}");
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        CancelCts();

        (DataContext as IPageLifecycleAwareBase)?.OnUnloading();
    }

    private void CancelCts()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
