using System.Collections.Concurrent;

namespace FluentBitwarden.Platform.Tests.Ipc.Infrastructure;

public sealed class RpcShapesHandlerState
{
    public int EchoInvocationCount { get; private set; }
    public int RequestCommandInvocationCount { get; private set; }
    public int CommandResponseInvocationCount { get; private set; }
    public int CommandInvocationCount { get; private set; }
    public EchoRequest LastEchoRequest { get; private set; }
    public CommandRequest LastCommandRequest { get; private set; }

    public void RecordEcho(EchoRequest request)
    {
        EchoInvocationCount++;
        LastEchoRequest = request;
    }

    public void RecordRequestCommand(CommandRequest request)
    {
        RequestCommandInvocationCount++;
        LastCommandRequest = request;
    }

    public void RecordCommandResponse() => CommandResponseInvocationCount++;

    public void RecordCommand() => CommandInvocationCount++;
}

public sealed class RpcShapesHandler(RpcShapesHandlerState state) : IIpcRequestsHandler
{
    public ValueTask<EchoResponse> Echo(EchoRequest request, CancellationToken cancellationToken)
    {
        state.RecordEcho(request);
        return ValueTask.FromResult(new EchoResponse(91, $"response:{request.Text}", 407));
    }

    public ValueTask Apply(CommandRequest request, CancellationToken cancellationToken)
    {
        state.RecordRequestCommand(request);
        return ValueTask.CompletedTask;
    }

    [IpcMessageHandler(TestMessageTypes.CommandResponse)]
    public ValueTask<EchoResponse> GetCommandResponse(CancellationToken cancellationToken)
    {
        state.RecordCommandResponse();
        return ValueTask.FromResult(new EchoResponse(17, "command-response", 3));
    }

    [IpcMessageHandler(TestMessageTypes.Command)]
    public ValueTask RunCommand(CancellationToken cancellationToken)
    {
        state.RecordCommand();
        return ValueTask.CompletedTask;
    }
}

public sealed class BlockingEchoHandlerState
{
    private readonly ConcurrentDictionary<int, RequestState> states = new();

    internal RequestState StateFor(int requestNumber) =>
        states.GetOrAdd(requestNumber, static _ => new RequestState());

    internal sealed class RequestState
    {
        public TaskCompletionSource<bool> Started { get; } = NewCompletionSource();
        public TaskCompletionSource<bool> Release { get; } = NewCompletionSource();
        public TaskCompletionSource<bool> Completed { get; } = NewCompletionSource();
        public TaskCompletionSource<bool> CancellationObserved { get; } = NewCompletionSource();

        private static TaskCompletionSource<bool> NewCompletionSource() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public async ValueTask<EchoResponse> WaitForReleaseAsync(
        EchoRequest request,
        CancellationToken cancellationToken)
    {
        RequestState state = StateFor(request.Number);
        state.Started.TrySetResult(true);
        using CancellationTokenRegistration registration = cancellationToken.Register(
            static state => ((RequestState)state!).CancellationObserved.TrySetResult(true),
            state);

        try
        {
            await state.Release.Task.WaitAsync(cancellationToken);
            return new EchoResponse(request.Number, $"handled:{request.Text}", request.Number + 1000);
        }
        finally
        {
            state.Completed.TrySetResult(true);
        }
    }
}

public sealed class BlockingEchoHandler(BlockingEchoHandlerState state) : IIpcRequestsHandler
{
    public ValueTask<EchoResponse> Echo(EchoRequest request, CancellationToken cancellationToken) =>
        state.WaitForReleaseAsync(request, cancellationToken);
}

public sealed class ImmediateEchoHandlerState
{
    public TaskCompletionSource<bool> Completed { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool HandlerTokenWasCancelled { get; private set; }
    public int InvocationCount { get; private set; }

    public void RecordInvocation(CancellationToken cancellationToken)
    {
        InvocationCount++;
        HandlerTokenWasCancelled = cancellationToken.IsCancellationRequested;
        Completed.TrySetResult(true);
    }
}

public sealed class ImmediateEchoHandler(ImmediateEchoHandlerState state) : IIpcRequestsHandler
{
    public ValueTask<EchoResponse> Echo(EchoRequest request, CancellationToken cancellationToken)
    {
        state.RecordInvocation(cancellationToken);
        return ValueTask.FromResult(new EchoResponse(request.Number, request.Text, request.Number));
    }
}

public sealed class ServerCancellingHandler : IIpcRequestsHandler
{
    public ValueTask<EchoResponse> Echo(
        EchoRequest request,
        CancellationToken cancellationToken)
    {
        throw new OperationCanceledException("Expected test handler cancellation.");
    }
}

public sealed class FailingRpcShapesHandler : IIpcRequestsHandler
{
    public ValueTask<EchoResponse> Echo(EchoRequest request, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Expected test handler failure.");

    public ValueTask Apply(CommandRequest request, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Expected test handler failure.");

    [IpcMessageHandler(TestMessageTypes.CommandResponse)]
    public ValueTask<EchoResponse> GetCommandResponse(CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Expected test handler failure.");

    [IpcMessageHandler(TestMessageTypes.Command)]
    public ValueTask RunCommand(CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Expected test handler failure.");
}

public sealed class FailOnceHandlerState
{
    private int invocationCount;

    public int InvocationCount => Volatile.Read(ref invocationCount);

    public bool ShouldFail() => Interlocked.Increment(ref invocationCount) == 1;
}

public sealed class FailOnceHandler(FailOnceHandlerState state) : IIpcRequestsHandler
{
    public ValueTask<EchoResponse> Echo(EchoRequest request, CancellationToken cancellationToken)
    {
        if (state.ShouldFail())
            throw new InvalidOperationException("Expected one-time test handler failure.");

        return ValueTask.FromResult(new EchoResponse(request.Number, request.Text, request.Number));
    }
}

public sealed class ScopedLifetimeProbe
{
    private readonly ConcurrentDictionary<int, TaskCompletionSource<bool>> disposals = new();
    private int nextInstanceId;
    private int disposalCount;

    public int DisposalCount => Volatile.Read(ref disposalCount);

    public int CreateInstance()
    {
        int instanceId = Interlocked.Increment(ref nextInstanceId);
        disposals[instanceId] = NewCompletionSource();
        return instanceId;
    }

    public Task DisposalFor(int instanceId) => disposals[instanceId].Task;

    public void RecordDisposal(int instanceId)
    {
        if (disposals[instanceId].TrySetResult(true))
            Interlocked.Increment(ref disposalCount);
    }

    private static TaskCompletionSource<bool> NewCompletionSource() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}

public sealed class ScopedRequestDependency(ScopedLifetimeProbe probe) : IAsyncDisposable
{
    public int InstanceId { get; } = probe.CreateInstance();

    public ValueTask DisposeAsync()
    {
        probe.RecordDisposal(InstanceId);
        return ValueTask.CompletedTask;
    }
}

public sealed class ScopedLifetimeHandler(ScopedRequestDependency dependency) : IIpcRequestsHandler
{
    public ValueTask<EchoResponse> Echo(EchoRequest request, CancellationToken cancellationToken) =>
        ValueTask.FromResult(new EchoResponse(request.Number, request.Text, dependency.InstanceId));
}

public sealed class ScopedRequestState
{
    private readonly TaskCompletionSource<bool> cancellationGate =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource<int> Started { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource<bool> Completed { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void RecordStarted(int instanceId) => Started.TrySetResult(instanceId);

    public void RecordCompleted() => Completed.TrySetResult(true);

    public Task WaitForCancellationAsync(CancellationToken cancellationToken) =>
        cancellationGate.Task.WaitAsync(cancellationToken);
}

public sealed class ScopedCancellationHandler(
    ScopedRequestDependency dependency,
    ScopedRequestState state)
    : IIpcRequestsHandler
{
    public async ValueTask<EchoResponse> Echo(
        EchoRequest request,
        CancellationToken cancellationToken)
    {
        state.RecordStarted(dependency.InstanceId);

        try
        {
            await state.WaitForCancellationAsync(cancellationToken);
            return new EchoResponse(request.Number, request.Text, dependency.InstanceId);
        }
        finally
        {
            state.RecordCompleted();
        }
    }
}

public sealed class ScopedThrowingHandler(
    ScopedRequestDependency dependency,
    ScopedRequestState state)
    : IIpcRequestsHandler
{
    public ValueTask<EchoResponse> Echo(
        EchoRequest request,
        CancellationToken cancellationToken)
    {
        state.RecordStarted(dependency.InstanceId);

        try
        {
            throw new InvalidOperationException("Expected test handler failure.");
        }
        finally
        {
            state.RecordCompleted();
        }
    }
}
