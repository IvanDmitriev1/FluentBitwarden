using CommunityToolkit.WinUI;
using Microsoft.Extensions.Hosting;

namespace FluentBitwarden.Ui.Infrastructure.Hosting;

internal sealed class WinUiHostedService<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TApplication>(
    Func<TApplication> createApplication,
    IHostApplicationLifetime lifetime,
    ILogger<WinUiHostedService<TApplication>> logger) : IHostedLifecycleService
    where TApplication : Microsoft.UI.Xaml.Application
{
    private readonly WinUiThread _thread = new(createApplication);

    public bool HasLiveDispatcher => _thread.Dispatcher is not null && !_thread.Completion.IsCompleted;

    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        _ = StopHostOnExitAsync();
        try
        {
            await _thread.StartAsync(cancellationToken);
        }
        catch (Exception startupError)
        {
            logger.LogError(startupError, "WinUI application failed to start.");
            using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try
            {
                await _thread.StopAsync(shutdown.Token);
            }
            catch (Exception shutdownError)
            {
                if (ReferenceEquals(startupError, shutdownError))
                {
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(startupError).Throw();
                }

                logger.LogError(shutdownError, "WinUI shutdown failed after startup failure.");
                throw new AggregateException("WinUI startup and shutdown both failed.", startupError, shutdownError);
            }

            throw;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => _thread.StopAsync(cancellationToken);

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void RunOnUiThread(Action action) => RunOnUiThreadAsync(action).SafeFireAndForget();

    public Task RunOnUiThreadAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return GetDispatcher().EnqueueAsync(action);
    }

    public Task RunOnUiThreadAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var dispatcher = GetDispatcher();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!dispatcher.TryEnqueue(async () =>
        {
            try
            {
                await action();
                completion.TrySetResult();
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
        }))
        {
            completion.TrySetException(new InvalidOperationException("The UI dispatcher is shutting down."));
        }

        return completion.Task;
    }

    public Task<T> RunOnUiThreadAsync<T>(Func<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return GetDispatcher().EnqueueAsync(action);
    }

    private Microsoft.UI.Dispatching.DispatcherQueue GetDispatcher()
    {
        if (_thread.Completion.IsCompleted)
        {
            _thread.Completion.GetAwaiter().GetResult();
            throw new InvalidOperationException("The WinUI application has exited.");
        }

        return _thread.Dispatcher ?? throw new InvalidOperationException("The UI dispatcher is not ready.");
    }

    private async Task StopHostOnExitAsync()
    {
        try
        {
            await _thread.Completion;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "WinUI application failed.");
        }
        finally
        {
            lifetime.StopApplication();
        }
    }
}
