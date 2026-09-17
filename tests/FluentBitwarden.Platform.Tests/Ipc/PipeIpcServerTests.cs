using FluentBitwarden.Platform.Ipc.Transport;
using FluentBitwarden.Platform.Tests.Ipc.Infrastructure;

namespace FluentBitwarden.Platform.Tests.Ipc;

public class PipeIpcServerTests
{
    [Fact(Timeout = 4000)]
    public async Task Rpc_shapes_round_trip_values_and_invoke_each_handler_once()
    {
        await using var host = await IpcTestHost.StartAsync<RpcShapesHandler>();
        var handler = host.Handler<RpcShapesHandler>();
        CancellationToken testCancellation = TestContext.Current.CancellationToken;

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
        Assert.Equal(request, handler.LastEchoRequest);
        Assert.Equal(new CommandRequest("sync", 12), handler.LastCommandRequest);
        Assert.Equal(1, handler.EchoInvocationCount);
        Assert.Equal(1, handler.RequestCommandInvocationCount);
        Assert.Equal(1, handler.CommandResponseInvocationCount);
        Assert.Equal(1, handler.CommandInvocationCount);
    }

    [Fact(Timeout = 4000)]
    public async Task Concurrent_calls_keep_responses_associated_when_released_in_reverse_order()
    {
        await using var host = await IpcTestHost.StartAsync<BlockingEchoHandler>();
        var handler = host.Handler<BlockingEchoHandler>();
        var firstState = handler.StateFor(1);
        var secondState = handler.StateFor(2);
        CancellationToken testCancellation = TestContext.Current.CancellationToken;

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

    [Fact(Timeout = 4000)]
    public async Task Cancelling_one_client_call_does_not_prevent_another_call_from_completing()
    {
        await using var host = await IpcTestHost.StartAsync<BlockingEchoHandler>();
        var handler = host.Handler<BlockingEchoHandler>();
        var cancelledState = handler.StateFor(3);
        var successfulState = handler.StateFor(4);
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
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

    [Fact(Timeout = 4000)]
    public async Task Normal_completion_does_not_observe_a_cancelled_handler_token()
    {
        await using var host = await IpcTestHost.StartAsync<ImmediateEchoHandler>();
        var handler = host.Handler<ImmediateEchoHandler>();
        CancellationToken testCancellation = TestContext.Current.CancellationToken;

        EchoResponse response = await host.Client.SendAsync<EchoRequest, EchoResponse>(
            new EchoRequest(6, "complete"),
            testCancellation);

        await handler.Completed.Task.WaitAsync(IpcTestHost.Timeout, testCancellation);

        Assert.Equal(new EchoResponse(6, "complete", 6), response);
        Assert.False(handler.HandlerTokenWasCancelled);
    }

    [Fact(Timeout = 4000)]
    public async Task Failed_response_write_after_client_disconnect_does_not_stop_the_accept_loop()
    {
        await using var host = await IpcTestHost.StartAsync<BlockingEchoHandler>();
        var handler = host.Handler<BlockingEchoHandler>();
        var state = handler.StateFor(7);
        var afterDisconnectState = handler.StateFor(8);
        CancellationToken testCancellation = TestContext.Current.CancellationToken;

        await using var disconnectedClient = await RawIpcClient.ConnectAsync(host.PipeName);
        await RawIpcClient.WriteRequestAsync(
            disconnectedClient,
            IpcConstants.ProtocolVersion,
            TestMessageTypes.Echo,
            MemoryPack.MemoryPackSerializer.Serialize(new EchoRequest(7, "disconnected")));
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
}
