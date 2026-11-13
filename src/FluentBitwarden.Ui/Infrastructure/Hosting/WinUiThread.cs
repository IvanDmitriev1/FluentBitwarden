using Microsoft.UI.Dispatching;

namespace FluentBitwarden.Ui.Infrastructure.Hosting;

internal sealed class WinUiThread(Func<Microsoft.UI.Xaml.Application> createApplication)
{
    private readonly Lock _lifecycleLock = new();
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private DispatcherQueue? _dispatcher;
    private Microsoft.UI.Xaml.Application? _application;
    private Thread? _thread;
    private bool _stopRequested;
    private Exception? _shutdownError;

    public DispatcherQueue? Dispatcher => Volatile.Read(ref _dispatcher);
    public Task Completion => _completed.Task;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        lock (_lifecycleLock)
        {
            if (_stopRequested)
                throw new InvalidOperationException("The WinUI thread is stopping.");
            if (_thread is not null)
                throw new InvalidOperationException("The WinUI thread has already started.");

            _thread = new Thread(Run)
            {
                Name = "WinUI Thread",
                IsBackground = true
            };

            try
            {
                _thread.SetApartmentState(ApartmentState.STA);
                _thread.Start();
            }
            catch (Exception exception)
            {
                _ready.TrySetException(exception);
                _completed.TrySetException(exception);
                throw;
            }
        }

        return _ready.Task.WaitAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_lifecycleLock)
        {
            if (_thread is null)
            {
                _stopRequested = true;
                _completed.TrySetResult();
            }
            else if (!_stopRequested)
            {
                _stopRequested = true;
                if (_dispatcher is { } dispatcher && _application is { } application && !dispatcher.TryEnqueue(application.Exit))
                {
                    _shutdownError = new InvalidOperationException("The UI dispatcher stopped before shutdown completed.");
                }
            }
        }

        return Completion.WaitAsync(cancellationToken);
    }

    private void Run()
    {
        Exception? runError = null;
        try
        {
            Microsoft.UI.Xaml.Application.Start(_ =>
            {
                var dispatcher = DispatcherQueue.GetForCurrentThread();
                SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(dispatcher));
                var application = createApplication();

                lock (_lifecycleLock)
                {
                    _application = application;
                    Volatile.Write(ref _dispatcher, dispatcher);
                    if (_stopRequested)
                    {
                        if (!dispatcher.TryEnqueue(application.Exit))
                            _shutdownError = new InvalidOperationException("The UI dispatcher stopped before shutdown completed.");
                    }
                    else
                    {
                        _ready.TrySetResult();
                    }
                }
            });

            _ready.TrySetException(new InvalidOperationException("The UI application exited before readiness."));
        }
        catch (Exception exception)
        {
            runError = exception;
            _ready.TrySetException(exception);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(null);
            lock (_lifecycleLock)
            {
                if (runError is not null && _shutdownError is not null)
                    _completed.TrySetException(new AggregateException(runError, _shutdownError));
                else if ((runError ?? _shutdownError) is { } exception)
                    _completed.TrySetException(exception);
                else
                    _completed.TrySetResult();
            }
        }
    }
}
