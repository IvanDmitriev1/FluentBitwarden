using System.Collections.Concurrent;

namespace FluentBitwarden.Platform.Tests.Ipc.Infrastructure;

public sealed class RpcShapesHandler : IIpcRequestsHandler
{
    public int EchoInvocationCount { get; private set; }
    public int RequestCommandInvocationCount { get; private set; }
    public int CommandResponseInvocationCount { get; private set; }
    public int CommandInvocationCount { get; private set; }
    public EchoRequest LastEchoRequest { get; private set; }
    public CommandRequest LastCommandRequest { get; private set; }

    public ValueTask<EchoResponse> Echo(EchoRequest request, CancellationToken cancellationToken)
    {
        EchoInvocationCount++;
        LastEchoRequest = request;
        return ValueTask.FromResult(new EchoResponse(91, $"response:{request.Text}", 407));
    }

    public ValueTask Apply(CommandRequest request, CancellationToken cancellationToken)
    {
        RequestCommandInvocationCount++;
        LastCommandRequest = request;
        return ValueTask.CompletedTask;
    }

    [IpcMessageHandler(TestMessageTypes.CommandResponse)]
    public ValueTask<EchoResponse> GetCommandResponse(CancellationToken cancellationToken)
    {
        CommandResponseInvocationCount++;
        return ValueTask.FromResult(new EchoResponse(17, "command-response", 3));
    }

    [IpcMessageHandler(TestMessageTypes.Command)]
    public ValueTask RunCommand(CancellationToken cancellationToken)
    {
        CommandInvocationCount++;
        return ValueTask.CompletedTask;
    }
}

public sealed class BlockingEchoHandler : IIpcRequestsHandler
{
    private readonly ConcurrentDictionary<int, RequestState> states = new();

    public ValueTask<EchoResponse> Echo(EchoRequest request, CancellationToken cancellationToken) =>
        WaitForReleaseAsync(request, cancellationToken);

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

    private async ValueTask<EchoResponse> WaitForReleaseAsync(
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

public sealed class ImmediateEchoHandler : IIpcRequestsHandler
{
    public TaskCompletionSource<bool> Completed { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool HandlerTokenWasCancelled { get; private set; }
    public int InvocationCount { get; private set; }

    public ValueTask<EchoResponse> Echo(EchoRequest request, CancellationToken cancellationToken)
    {
        InvocationCount++;
        HandlerTokenWasCancelled = cancellationToken.IsCancellationRequested;
        Completed.TrySetResult(true);
        return ValueTask.FromResult(new EchoResponse(request.Number, request.Text, request.Number));
    }
}
