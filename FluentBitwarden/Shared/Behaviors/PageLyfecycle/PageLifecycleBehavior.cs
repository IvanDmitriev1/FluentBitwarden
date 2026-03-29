using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Microsoft.UI.Xaml;
using Microsoft.Xaml.Interactivity;

namespace FluentBitwarden.Shared.Behaviors.PageLyfecycle;

public sealed class PageLifecycleBehavior : Behavior<Page>
{
    private readonly PageLoadCoordinator _loadCoordinator = new();

    protected override void OnAttached()
    {
        AssociatedObject.Loaded += OnAssociatedObjectLoaded;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.Loaded -= OnAssociatedObjectLoaded;

        CancelPageLoading();
        SetRecipientActive(isActive: false);

        if (AssociatedObject.DataContext is IPageLifecycleAware vm)
        {
            vm.OnUnloading();
        }
    }

    private async void OnAssociatedObjectLoaded(object sender, RoutedEventArgs e)
    {
        SetRecipientActive(isActive: true);

        if (AssociatedObject.DataContext is IPageLifecycleAware vm)
        {
            await OnPageLoading(vm);
        }
    }

    private async ValueTask OnPageLoading(IPageLifecycleAware vm)
    {
        var load = _loadCoordinator.Start();

        try
        {
            await vm.OnLoadingAsync(load.CancellationToken);
        }
        catch (OperationCanceledException ex) when (!_loadCoordinator.IsCurrent(load) || load.CancellationToken.IsCancellationRequested)
        {
            Debug.WriteLine($"Canceled page loading for {AssociatedObject.GetType().Name}: {ex.Message}");
        }
        catch (Exception ex) when (!_loadCoordinator.IsCurrent(load))
        {
            Debug.WriteLine($"Ignoring stale page loading failure for {AssociatedObject.GetType().Name}: {ex.Message}");
        }
        catch (Exception ex)
        {
            AssociatedObject.DispatcherQueue.TryEnqueue(() => ExceptionDispatchInfo.Throw(ex));
        }
        finally
        {
            _loadCoordinator.Complete(load);
        }
    }

    private void CancelPageLoading()
    {
        _loadCoordinator.CancelCurrent();
    }

    private void SetRecipientActive(bool isActive)
    {
        if (AssociatedObject.DataContext is ObservableRecipient recipient)
        {
            recipient.IsActive = isActive;
        }
    }
}
