using System.Runtime.ExceptionServices;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace FluentBitwarden.Shared.Behaviors;

public sealed class PageLifecycleBehavior : Behavior<Page>
{
    private CancellationTokenSource? _cts;

    protected override void OnAttached()
    {
        if (AssociatedObject.DataContext is not IPageLifecycleAware vm)
            return;

        AssociatedObject.Loaded += async (sender, args) =>
        {
            await OnPageLoading(vm);
        };
    }

    protected override async void OnDetaching()
    {
        if (AssociatedObject.DataContext is not IPageLifecycleAware vm)
            return;

        await OnPageUnLoading(vm);
    }

    private async Task OnPageUnLoading(IPageLifecycleAware vm)
    {
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
            //
        }
        catch (Exception ex)
        {
            AssociatedObject.DispatcherQueue.TryEnqueue(() => ExceptionDispatchInfo.Throw(ex));
        }
    }
}