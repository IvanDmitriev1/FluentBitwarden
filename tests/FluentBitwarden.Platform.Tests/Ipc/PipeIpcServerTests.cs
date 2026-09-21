using FluentBitwarden.Platform.Ipc.Transport;
using FluentBitwarden.Platform.Tests.Ipc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Platform.Tests.Ipc;

public class PipeIpcServerTests
{
    [Fact(Timeout = IpcTestHost.TimeoutMilliseconds)]
    public async Task Rpc_shapes_round_trip_values_and_invoke_each_handler_once()
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        var state = new RpcShapesHandlerState();
        await using var host = await IpcTestHost.StartAsync<RpcShapesHandler>(
            configureServices: services => services.AddSingleton(state),
            cancellationToken: testCancellation);

        EchoRequest request = new(73, "wire-value");
        EchoResponse response = await host.Client.SendAsync<EchoRequest, EchoResponse>(
            request,
            testCancellation);
        IpcVoid requestCommandResponse = await host.Client.SendAsync<CommandRequest, IpcVoid>(
            new CommandRequest("sync", 12),
            testCancellation);
        EchoResponse commandResponse = await host.Client.SendAsync<EchoResponse>(
            TestMessageTypes.CommandResponse,
            testCancellation);
        IpcVoid commandResult = await host.Client.SendAsync<IpcVoid>(
            TestMessageTypes.Command,
            testCancellation);

        Assert.Equal(new EchoResponse(91, "response:wire-value", 407), response);
        Assert.Equal(IpcVoid.Value, requestCommandResponse);
        Assert.Equal(new EchoResponse(17, "command-response", 3), commandResponse);
        Assert.Equal(IpcVoid.Value, commandResult);
        Assert.Equal(request, state.LastEchoRequest);
        Assert.Equal(new CommandRequest("sync", 12), state.LastCommandRequest);
        Assert.Equal(1, state.EchoInvocationCount);
        Assert.Equal(1, state.RequestCommandInvocationCount);
        Assert.Equal(1, state.CommandResponseInvocationCount);
        Assert.Equal(1, state.CommandInvocationCount);
    }

    [Fact(Timeout = IpcTestHost.TimeoutMilliseconds)]
    public async Task Concurrent_calls_keep_responses_associated_when_released_in_reverse_order()
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        var handlerState = new BlockingEchoHandlerState();
        await using var host = await IpcTestHost.StartAsync<BlockingEchoHandler>(
            configureServices: services => services.AddSingleton(handlerState),
            cancellationToken: testCancellation);
        var firstState = handlerState.StateFor(1);
        var secondState = handlerState.StateFor(2);

        Task<EchoResponse> first = host.Client.SendAsync<EchoRequest, EchoResponse>(
            new EchoRequest(1, "first"),
            testCancellation).AsTask();
        Task<EchoResponse> second = host.Client.SendAsync<EchoRequest, EchoResponse>(
            new EchoRequest(2, "second"),
            testCancellation).AsTask();

        await firstState.Started.Task.WaitAsync(IpcTestHost.Timeout, testCancellation);
        await secondState.Started.Task.WaitAsync(IpcTestHost.Timeout, testCancellation);

        secondState.Release.TrySetResult(true);
        Assert.Equal(
            new EchoResponse(2, "handled:second", 1002),
            await second.WaitAsync(IpcTestHost.Timeout, testCancellation));

        firstState.Release.TrySetResult(true);
        Assert.Equal(
            new EchoResponse(1, "handled:first", 1001),
            await first.WaitAsync(IpcTestHost.Timeout, testCancellation));
    }

    [Fact(Timeout = IpcTestHost.TimeoutMilliseconds)]
    public async Task Cancelling_one_client_call_does_not_prevent_another_call_from_completing()
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        var handlerState = new BlockingEchoHandlerState();
        await using var host = await IpcTestHost.StartAsync<BlockingEchoHandler>(
            configureServices: services => services.AddSingleton(handlerState),
            cancellationToken: testCancellation);
        var cancelledState = handlerState.StateFor(3);
        var successfulState = handlerState.StateFor(4);
        using var cancellation = new CancellationTokenSource();

        Task<EchoResponse> cancelled = host.Client.SendAsync<EchoRequest, EchoResponse>(
            new EchoRequest(3, "cancelled"),
            cancellation.Token).AsTask();
        Task<EchoResponse> successful = host.Client.SendAsync<EchoRequest, EchoResponse>(
            new EchoRequest(4, "successful"),
            testCancellation).AsTask();

        try
        {
            await cancelledState.Started.Task.WaitAsync(IpcTestHost.Timeout, testCancellation);
            await successfulState.Started.Task.WaitAsync(IpcTestHost.Timeout, testCancellation);

            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await cancelled.WaitAsync(IpcTestHost.Timeout, testCancellation));

            successfulState.Release.TrySetResult(true);
            Assert.Equal(
                new EchoResponse(4, "handled:successful", 1004),
                await successful.WaitAsync(IpcTestHost.Timeout, testCancellation));
        }
        finally
        {
            cancelledState.Release.TrySetResult(true);
            successfulState.Release.TrySetResult(true);
            await cancelledState.Completed.Task.WaitAsync(IpcTestHost.Timeout, testCancellation);
            await successfulState.Completed.Task.WaitAsync(IpcTestHost.Timeout, testCancellation);
        }
    }

    [Fact(Timeout = IpcTestHost.TimeoutMilliseconds)]
    public async Task Normal_completion_does_not_observe_a_cancelled_handler_token()
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        var state = new ImmediateEchoHandlerState();
        await using var host = await IpcTestHost.StartAsync<ImmediateEchoHandler>(
            configureServices: services => services.AddSingleton(state),
            cancellationToken: testCancellation);

        EchoResponse response = await host.Client.SendAsync<EchoRequest, EchoResponse>(
            new EchoRequest(6, "complete"),
            testCancellation);

        await state.Completed.Task.WaitAsync(IpcTestHost.Timeout, testCancellation);

        Assert.Equal(new EchoResponse(6, "complete", 6), response);
        Assert.False(state.HandlerTokenWasCancelled);
    }

    [Fact(Timeout = IpcTestHost.TimeoutMilliseconds)]
    public async Task Server_cancelled_request_is_reported_as_operation_canceled_to_client()
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        await using var host = await IpcTestHost.StartAsync<ServerCancellingHandler>(
            cancellationToken: testCancellation);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            host.Client.SendAsync<EchoRequest, EchoResponse>(
                new EchoRequest(9, "server-cancelled"),
                testCancellation).AsTask());
    }

    [Fact(Timeout = IpcTestHost.TimeoutMilliseconds)]
    public async Task Failed_response_write_after_client_disconnect_does_not_stop_the_accept_loop()
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        var handlerState = new BlockingEchoHandlerState();
        await using var host = await IpcTestHost.StartAsync<BlockingEchoHandler>(
            configureServices: services => services.AddSingleton(handlerState),
            cancellationToken: testCancellation);
        var state = handlerState.StateFor(7);
        var afterDisconnectState = handlerState.StateFor(8);

        await using var disconnectedClient = await RawIpcClient.ConnectAsync(
            host.PipeName,
            testCancellation);
        await RawIpcClient.WriteRequestAsync(
            disconnectedClient,
            IpcConstants.ProtocolVersion,
            TestMessageTypes.Echo,
            MemoryPack.MemoryPackSerializer.Serialize(new EchoRequest(7, "disconnected")),
            testCancellation);
        await state.Started.Task.WaitAsync(IpcTestHost.Timeout, testCancellation);

        await disconnectedClient.DisposeAsync();
        state.Release.TrySetResult(true);
        await state.Completed.Task.WaitAsync(IpcTestHost.Timeout, testCancellation);

        Task<EchoResponse> responseTask = host.Client.SendAsync<EchoRequest, EchoResponse>(
            new EchoRequest(8, "after-disconnect"),
            testCancellation).AsTask();

        await afterDisconnectState.Started.Task.WaitAsync(IpcTestHost.Timeout, testCancellation);
        afterDisconnectState.Release.TrySetResult(true);

        EchoResponse response = await responseTask.WaitAsync(TimeSpan.FromSeconds(1), testCancellation);

        Assert.Equal(new EchoResponse(8, "handled:after-disconnect", 1008), response);
    }

    [Fact(Timeout = IpcTestHost.TimeoutMilliseconds)]
    public async Task Requests_use_distinct_scoped_dependencies_and_dispose_each_after_response()
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        var probe = new ScopedLifetimeProbe();
        await using var host = await IpcTestHost.StartAsync<ScopedLifetimeHandler>(
            configureServices: services =>
            {
                services.AddSingleton(probe);
                services.AddScoped<ScopedRequestDependency>();
            },
            cancellationToken: testCancellation);

        EchoResponse first = await host.Client.SendAsync<EchoRequest, EchoResponse>(
            new EchoRequest(31, "first-scope"),
            testCancellation);
        await probe.DisposalFor(first.Count).WaitAsync(IpcTestHost.Timeout, testCancellation);

        EchoResponse second = await host.Client.SendAsync<EchoRequest, EchoResponse>(
            new EchoRequest(32, "second-scope"),
            testCancellation);
        await probe.DisposalFor(second.Count).WaitAsync(IpcTestHost.Timeout, testCancellation);

        Assert.NotEqual(first.Count, second.Count);
        Assert.Equal(2, probe.DisposalCount);
    }

    [Fact(Timeout = IpcTestHost.TimeoutMilliseconds)]
    public async Task Cancelled_request_disposes_its_scoped_dependencies()
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        using var requestCancellation = new CancellationTokenSource();
        var probe = new ScopedLifetimeProbe();
        var state = new ScopedRequestState();
        await using var host = await IpcTestHost.StartAsync<ScopedCancellationHandler>(
            configureServices: services =>
            {
                services.AddSingleton(probe);
                services.AddSingleton(state);
                services.AddScoped<ScopedRequestDependency>();
            },
            cancellationToken: testCancellation);

        Task<EchoResponse> request = host.Client.SendAsync<EchoRequest, EchoResponse>(
            new EchoRequest(41, "cancel-scope"),
            requestCancellation.Token).AsTask();
        int instanceId = await state.Started.Task.WaitAsync(
            IpcTestHost.Timeout,
            testCancellation);

        requestCancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await request.WaitAsync(IpcTestHost.Timeout, testCancellation));
        await state.Completed.Task.WaitAsync(IpcTestHost.Timeout, testCancellation);
        await probe.DisposalFor(instanceId).WaitAsync(IpcTestHost.Timeout, testCancellation);

        Assert.Equal(1, probe.DisposalCount);
    }

    [Fact(Timeout = IpcTestHost.TimeoutMilliseconds)]
    public async Task Failed_request_disposes_its_scoped_dependencies()
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        var probe = new ScopedLifetimeProbe();
        var state = new ScopedRequestState();
        await using var host = await IpcTestHost.StartAsync<ScopedThrowingHandler>(
            configureServices: services =>
            {
                services.AddSingleton(probe);
                services.AddSingleton(state);
                services.AddScoped<ScopedRequestDependency>();
            },
            cancellationToken: testCancellation);

        Task<EchoResponse> request = host.Client.SendAsync<EchoRequest, EchoResponse>(
            new EchoRequest(42, "fail-scope"),
            testCancellation).AsTask();
        int instanceId = await state.Started.Task.WaitAsync(
            IpcTestHost.Timeout,
            testCancellation);

        await Assert.ThrowsAnyAsync<IOException>(async () =>
            await request.WaitAsync(IpcTestHost.Timeout, testCancellation));
        await state.Completed.Task.WaitAsync(IpcTestHost.Timeout, testCancellation);
        await probe.DisposalFor(instanceId).WaitAsync(IpcTestHost.Timeout, testCancellation);

        Assert.Equal(1, probe.DisposalCount);
    }
}
