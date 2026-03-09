using System.Diagnostics;
using FluentBitwarden.Ui.Abstractions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Ui.Controls;

public abstract class CorePage : Page
{
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private readonly IPageLifecycleAware? _lifecycleAware;
    private CancellationTokenSource? _lifecycleCts;
    private bool _disposed;

    protected CorePage(object viewModel)
    {
        DataContext = viewModel;
        _lifecycleAware = viewModel as IPageLifecycleAware;

        Loading += OnPageLoading;
        Unloaded += OnPageUnloaded;
    }

    private async void OnPageLoading(FrameworkElement sender, object args)
    {
        if (_disposed)
        {
            return;
        }

        await ExecuteLoadingAsync();
    }

    private async void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        await ExecuteUnloadingAsync();

        if (NavigationCacheMode == Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Disabled)
        {
            Dispose();
        }
    }

    private async Task ExecuteLoadingAsync()
    {
        await ExecuteLifecycleAsync(
            lifecycle: static (aware, token) => aware.OnLoadingAsync(token),
            phaseName: "loading");
    }

    private async Task ExecuteUnloadingAsync()
    {
        await ExecuteLifecycleAsync(
            lifecycle: static (aware, token) => aware.OnUnloadingAsync(token),
            phaseName: "unloading");
    }

    private async Task ExecuteLifecycleAsync(
        Func<IPageLifecycleAware, CancellationToken, Task> lifecycle,
        string phaseName)
    {
        try
        {
            await _lifecycleGate.WaitAsync();
        }
        catch (ObjectDisposedException)
        {
            Debug.WriteLine($"{GetType().Name}: lifecycle gate already disposed.");
            return;
        }

        try
        {
            if (_disposed)
            {
                return;
            }

            if (_lifecycleAware is null)
            {
                return;
            }

            var token = RestartLifecycleToken();

            try
            {
                await lifecycle(_lifecycleAware, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                Debug.WriteLine($"{GetType().Name}: {phaseName} lifecycle canceled.");
            }
        }
        finally
        {
            try
            {
                _lifecycleGate.Release();
            }
            catch (ObjectDisposedException)
            {
                Debug.WriteLine($"{GetType().Name}: lifecycle gate disposed during release.");
            }
        }
    }

    private CancellationToken RestartLifecycleToken()
    {
        _lifecycleCts?.Cancel();
        _lifecycleCts?.Dispose();
        _lifecycleCts = new CancellationTokenSource();
        return _lifecycleCts.Token;
    }

    private void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        Loading -= OnPageLoading;
        Unloaded -= OnPageUnloaded;

        _lifecycleCts?.Cancel();
        _lifecycleCts?.Dispose();
        _lifecycleCts = null;

        _lifecycleGate.Dispose();
    }
}
