using System.IO.Pipes;
using System.Runtime.ExceptionServices;
using FluentBitwarden.Platform.Ipc.Models;
using FluentBitwarden.Platform.Tests.Ipc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Platform.Tests.Ipc;

public class IpcRpcEndpointTests
{
    [Fact(Timeout = IpcTestHost.TimeoutMilliseconds)]
    public async Task Synchronous_request_completes_without_throwing_cancellation_exceptions()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string pipeName = $"FluentBitwarden.Tests.{Guid.NewGuid():N}";
        await using var server = new NamedPipeServerStream(
            pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous,
            inBufferSize: 4096, outBufferSize: 4096);
        await using var client = new NamedPipeClientStream(
            ".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        Task connection = server.WaitForConnectionAsync(cancellationToken);
        await client.ConnectAsync(cancellationToken);
        await connection;

        var state = new ImmediateEchoHandlerState();
        var services = new ServiceCollection();
        services.AddSingleton(state);
        var builder = new IpcRpcHandlerBuilder(services);
        builder.Add<ImmediateEchoHandler>();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        IpcRpcEndpoint endpoint = builder.Build()[TestMessageTypes.Echo];
        byte[] payload = MemoryPack.MemoryPackSerializer.Serialize(new EchoRequest(6, "complete"));
        for (int i = 0; i < 16; i++)
            await endpoint.InvokeAsync(scope.ServiceProvider, server, payload, cancellationToken);

        var exceptions = new AsyncLocal<int[]> { Value = new int[1] };
        int[] count = exceptions.Value;

        void OnException(object? sender, FirstChanceExceptionEventArgs args)
        {
            if (exceptions.Value == count && args.Exception is OperationCanceledException)
                Interlocked.Increment(ref count[0]);
        }

        long allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        AppDomain.CurrentDomain.FirstChanceException += OnException;
        try
        {
            for (int i = 0; i < 256; i++)
            {
                IpcRpcInvocationResult result = await endpoint.InvokeAsync(
                    scope.ServiceProvider, server, payload, cancellationToken);
                Assert.Equal(IpcRpcInvocationStatus.Success, result.Status);
            }
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= OnException;
        }

        long allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
        TestContext.Current.TestOutputHelper!.WriteLine($"256 invocations: {allocated} allocated bytes; {count[0]} cancellation throws.");
        Assert.Equal(272, state.InvocationCount);
        Assert.Equal(0, count[0]);
    }

    [Theory(Timeout = IpcTestHost.TimeoutMilliseconds)]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Disconnected_clients_or_extra_data_do_not_invoke_the_handler(bool extraData)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string pipeName = $"FluentBitwarden.Tests.{Guid.NewGuid():N}";
        await using var server = new NamedPipeServerStream(
            pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous,
            inBufferSize: 4096, outBufferSize: 4096);
        await using var client = new NamedPipeClientStream(
            ".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        Task connection = server.WaitForConnectionAsync(cancellationToken);
        await client.ConnectAsync(cancellationToken);
        await connection;

        if (extraData)
            await client.WriteAsync(new byte[] { 1 }, cancellationToken);
        else
            await client.DisposeAsync();

        var state = new ImmediateEchoHandlerState();
        var services = new ServiceCollection();
        services.AddSingleton(state);
        var builder = new IpcRpcHandlerBuilder(services);
        builder.Add<ImmediateEchoHandler>();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        IpcRpcEndpoint endpoint = builder.Build()[TestMessageTypes.Echo];

        IpcRpcInvocationResult result = await endpoint.InvokeAsync(
            scope.ServiceProvider,
            server,
            MemoryPack.MemoryPackSerializer.Serialize(new EchoRequest(6, "complete")),
            cancellationToken);

        Assert.Equal(IpcRpcInvocationStatus.NoResponse, result.Status);
        Assert.Equal(0, state.InvocationCount);
    }
}
