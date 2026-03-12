using System.Diagnostics;
using FluentBitwarden.Ui.Abstractions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Xaml.Interactivity;
using System.Runtime.ExceptionServices;

namespace FluentBitwarden.Ui.Behaviors;

public sealed class PageLifecycleBehavior : Behavior<Page>
{
    private CancellationTokenSource? _cts;
    private Frame? _frame;

    protected override void OnAttached()
    {
        AssociatedObject.Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        AssociatedObject.Loaded -= OnPageLoaded;

        _frame = AssociatedObject.Frame;
        Debug.Assert(_frame is not null, "_frame is null");

        _frame.Navigated += OnFrameNavigated;
        _frame.Navigating += OnFrameNavigating;

        if (AssociatedObject.DataContext is not IPageLifecycleAware vm)
            return;

        await OnPageLoading(vm);
    }

    protected override void OnDetaching()
    {
        AssociatedObject.Loaded -= OnPageLoaded;

        Debug.Assert(_frame is not null, "_frame is null");
        _frame.Navigated -= OnFrameNavigated;
        _frame.Navigating -= OnFrameNavigating;
        _frame = null;
    }

    private async void OnFrameNavigated(object sender, NavigationEventArgs e)
    {
        if (AssociatedObject.DataContext is not IPageLifecycleAware vm || ReferenceEquals(e.Content, AssociatedObject))
            return;

        await OnPageLoading(vm);
    }

    private async void OnFrameNavigating(object sender, NavigatingCancelEventArgs e)
    {
        if (AssociatedObject.DataContext is not IPageLifecycleAware vm || ReferenceEquals(_frame?.Content, AssociatedObject))
            return;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        try
        {
            await vm.OnUnloadingAsync();
        }
        catch (Exception ex)
        {
            AssociatedObject.DispatcherQueue.TryEnqueue(() => ExceptionDispatchInfo.Throw(ex));
        }
    }

    private async ValueTask OnPageLoading(IPageLifecycleAware vm)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        try
        {
            await vm.OnLoadingAsync(_cts.Token);
        }
        catch (OperationCanceledException)
        {

        }
        catch (Exception ex)
        {
            AssociatedObject.DispatcherQueue.TryEnqueue(() => ExceptionDispatchInfo.Throw(ex));
        }
    }
}