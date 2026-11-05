using System.Diagnostics.CodeAnalysis;
using AsyncAwaitBestPractices;
using CommunityToolkit.WinUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Ui.Infrastracture.Hosting;

internal sealed class WinUiHostedService<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TApplication>(
    IServiceProvider services,
    IHostApplicationLifetime lifetime,
    ILogger<WinUiHostedService<TApplication>> logger) : IHostedService
    where TApplication : Application
{
    private readonly TaskCompletionSource _ready = new(
        TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly TaskCompletionSource _stopped = new(
        TaskCreationOptions.RunContinuationsAsynchronously);

    private DispatcherQueue? _dispatcher;
    private TApplication? _application;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var thread = new Thread(RunApplication)
        {
            Name = "WinUI Thread",
            IsBackground = true
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return _ready.Task.WaitAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var dispatcher = Volatile.Read(ref _dispatcher);
        if (!_stopped.Task.IsCompleted && dispatcher is not null)
        {
            dispatcher.TryEnqueue(() => _application?.Exit());
        }

        await _stopped.Task.WaitAsync(cancellationToken);
    }

    public void RunOnUiThread(Action action)
    {
        ArgumentNullException.ThrowIfNull(_dispatcher);

        _dispatcher.EnqueueAsync(action).SafeFireAndForget();
    }

    private void RunApplication()
    {
        try
        {
            Application.Start(_ =>
            {
                var dispatcher = DispatcherQueue.GetForCurrentThread();
                var context = new DispatcherQueueSynchronizationContext(dispatcher);
                SynchronizationContext.SetSynchronizationContext(context);

                Volatile.Write(ref _dispatcher, dispatcher);
                _application = ActivatorUtilities.CreateInstance<TApplication>(services);

                _ready.TrySetResult();
            });
        }
        catch (Exception e)
        {
            logger.LogError(e, "WinUI application failed.");
            _ready.TrySetException(e);
        }
        finally
        {
            _stopped.TrySetResult();
            lifetime.StopApplication();
        }
    }
}
