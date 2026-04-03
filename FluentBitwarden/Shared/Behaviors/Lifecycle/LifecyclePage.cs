using Microsoft.UI.Xaml.Navigation;

namespace FluentBitwarden.Shared.Behaviors.Lifecycle;

public abstract class LifecyclePage : Page
{
    private CancellationTokenSource? _cts;

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            Task loadingTask = Task.CompletedTask;

            if (e.Parameter is IPageNavigationParameter parameter)
                loadingTask = parameter.Load(DataContext, token);
            else if (DataContext is IPageLifecycleAware aware)
                loadingTask = aware.OnLoadingAsync(token);

            await loadingTask;
        }
        catch (OperationCanceledException) { }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        if (DataContext is IPageLifecycleAwareBase aware)
            aware.OnUnloading();
    }
}
