using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;

namespace FluentBitwarden.Shared.Behaviors.Lifecycle;

public abstract class LifecyclePage : Page
{
    private CancellationTokenSource? _cts;
    private IPageNavigationParameter? _pendingParameter;

    protected LifecyclePage()
    {
        Loaded += OnLoaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _pendingParameter = e.Parameter as IPageNavigationParameter;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            Task loadingTask = Task.CompletedTask;
            if (_pendingParameter is not null)
                loadingTask = _pendingParameter.Load(DataContext, token);
            else if (DataContext is IPageLifecycleAware aware)
                loadingTask = aware.OnLoadingAsync(token);
            await loadingTask;
        }
        catch (OperationCanceledException) { }
        finally
        {
            _pendingParameter = null;
        }
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