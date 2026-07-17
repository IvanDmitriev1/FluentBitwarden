using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

namespace FluentBitwarden.Platform.Infrastructure.Clipboard;

internal sealed class ClipboardStaExecutor : IDisposable
{
    private readonly BlockingCollection<WorkItem> _workItems = new();
    private readonly Thread _thread;

    public ClipboardStaExecutor()
    {
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "FluentBitwarden clipboard STA",
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    public void Invoke(Action action)
    {
        if (Environment.CurrentManagedThreadId == _thread.ManagedThreadId)
        {
            action();
            return;
        }

        using var completed = new ManualResetEventSlim();
        var item = new WorkItem(action, completed);
        _workItems.Add(item);
        completed.Wait();

        if (item.Exception is not null)
            ExceptionDispatchInfo.Capture(item.Exception).Throw();
    }

    public void Post(Action action) => _workItems.Add(new WorkItem(action, null));

    public void Dispose()
    {
        _workItems.CompleteAdding();
        _workItems.Dispose();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Runs arbitrary caller delegates on a dedicated STA thread; any failure must be captured and rethrown on the caller's thread, not crash the STA loop.")]
    private void Run()
    {
        foreach (var item in _workItems.GetConsumingEnumerable())
        {
            try
            {
                item.Action();
            }
            catch (Exception exception)
            {
                item.Exception = exception;
                if (item.Completed is null)
                    Debug.WriteLine($"Clipboard work item failed: {exception.Message}");
            }
            finally
            {
                item.Completed?.Set();
            }
        }
    }

    private sealed class WorkItem(Action action, ManualResetEventSlim? completed)
    {
        public Action Action { get; } = action;
        public ManualResetEventSlim? Completed { get; } = completed;
        public Exception? Exception { get; set; }
    }
}