namespace FluentBitwarden.Shared.Behaviors.PageLyfecycle;

internal sealed class PageLoadCoordinator
{
    private ulong _generation;
    private CancellationTokenSource? _currentCancellation;

    public PageLoadInvocation Start()
    {
        CancelCurrent();

        var cancellation = new CancellationTokenSource();
        _currentCancellation = cancellation;
        _generation++;

        return new PageLoadInvocation(_generation, cancellation.Token);
    }

    public void CancelCurrent()
    {
        if (_currentCancellation is null)
        {
            return;
        }

        _generation++;
        _currentCancellation.Cancel();
        _currentCancellation.Dispose();
        _currentCancellation = null;
    }

    public bool IsCurrent(PageLoadInvocation invocation) => invocation.Generation == _generation;

    public void Complete(PageLoadInvocation invocation)
    {
        if (!IsCurrent(invocation))
        {
            return;
        }

        _currentCancellation?.Dispose();
        _currentCancellation = null;
    }
}
