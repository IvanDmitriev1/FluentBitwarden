using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;

namespace FluentBitwarden.Infrastructure.Navigation.Lifecycle;

public abstract class LifecyclePage : Page
{
    private CancellationTokenSource? _cts;
    private IPageNavigationParameter? _pendingParameter;
    private bool _isLoaded;
    private bool _hasPendingNavigation;

    protected LifecyclePage()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var parameter = e.Parameter as IPageNavigationParameter;
        if (_isLoaded)
        {
            LoadViewModel(parameter);
        }
        else
        {
            _hasPendingNavigation = true;
            _pendingParameter = parameter;
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        CancelLoading();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;

        if (DataContext is ObservableRecipient recipient)
            recipient.IsActive = true;

        if (!_hasPendingNavigation)
            return;

        _hasPendingNavigation = false;
        var param = _pendingParameter;
        _pendingParameter = null;

        LoadViewModel(param);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = false;
        CancelLoading();

        if (DataContext is ObservableRecipient recipient)
            recipient.IsActive = false;

        if (DataContext is IPageLifecycleAwareBase aware)
            aware.OnUnloading();
    }

    private async void LoadViewModel(IPageNavigationParameter? navParameter)
    {
        CancelLoading();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            Task loadingTask = Task.CompletedTask;
            if (navParameter is not null)
                loadingTask = navParameter.LoadAsync(DataContext, token);
            else if (DataContext is IPageLifecycleAware aware)
                loadingTask = aware.OnLoadingAsync(token);

            await loadingTask;
        }
        catch (Exception e) when (token.IsCancellationRequested &&
                                  e is TaskCanceledException or OperationCanceledException)
        {
            //
        }
        catch (Exception e)
        {
            UnhandledExceptionLogger.WriteException(e);
        }
    }

    private void CancelLoading()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
