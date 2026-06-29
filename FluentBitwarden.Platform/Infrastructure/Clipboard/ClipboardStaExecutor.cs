using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;

namespace FluentBitwarden.Platform.Infrastructure.Clipboard;

internal sealed class ClipboardStaExecutor
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