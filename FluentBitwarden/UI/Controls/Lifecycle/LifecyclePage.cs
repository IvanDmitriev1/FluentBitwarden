using FluentBitwarden.Application.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;

namespace FluentBitwarden.UI.Controls.Lifecycle;

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
        _pendingParameter = e.Parameter as IPageNavigationParameter;
        _hasPendingNavigation = true;

        if (_isLoaded)
        {
            StartLoading();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;

        if (_hasPendingNavigation)
        {
            StartLoading();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = false;
        CancelLoading();
    }

    private async void StartLoading()
    {
        CancelLoading();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        var parameter = _pendingParameter;

        _pendingParameter = null;
        _hasPendingNavigation = false;

        try
        {
            Task loadingTask = Task.CompletedTask;
            if (parameter is not null)
                loadingTask = parameter.Load(DataContext, token);
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

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        CancelLoading();

        if (DataContext is IPageLifecycleAwareBase aware)
            aware.OnUnloading();
    }

    private void CancelLoading()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
