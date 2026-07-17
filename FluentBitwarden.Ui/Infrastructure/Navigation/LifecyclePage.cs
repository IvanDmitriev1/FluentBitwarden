using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;

namespace FluentBitwarden.Infrastructure.Navigation;

[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable",
    Justification = "_cts is disposed via CancelLoading(), invoked from OnNavigatedFrom/Unloaded; Page has no IDisposable contract WinUI ever calls.")]
public abstract class LifecyclePage : Page, ILifeCycleAwarePage
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

    public void Reload(IPageNavigationParameter? parameter) => LoadViewModel(parameter);

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;

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

        if (DataContext is IPageLifecycleAwareBase aware)
            aware.OnUnloading();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "async void navigation entry point; unhandled exceptions here would crash the process instead of being logged.")]
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
        catch (OperationCanceledException)
        {
            //
        }
        catch (ArgumentOutOfRangeException)
        {
            Debug.Fail("Encountered unexpected navigation parameter!");
        }
        catch (Exception e)
        {
            App.Current.GetRequiredService<ILogger<LifecyclePage>>().PageLoadFailed(e);
        }
    }

    private void CancelLoading()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
