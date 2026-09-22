using FluentBitwarden.Platform.Tests.Ipc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FluentBitwarden.Platform.Tests.Ipc;

public class IpcPayloadValidationTests
{
    public static TheoryData<string, byte[]> ServerRejectedRequests => new()
    {
        {
            "unsupported protocol version",
            RawIpcClient.RequestHeader(
                (ushort)(IpcConstants.ProtocolVersion + 1),
                TestMessageTypes.Echo,
                payloadLength: 0)
        },
        {
            "negative payload length",
            RawIpcClient.RequestHeader(
                IpcConstants.ProtocolVersion,
                TestMessageTypes.Echo,
                payloadLength: -1)
        },
        {
            "unknown message id",
            RawIpcClient.RequestHeader(
                IpcConstants.ProtocolVersion,
                ushort.MaxValue,
                payloadLength: 0)
        },
    };

    public static TheoryData<string, byte[]> IncompleteRequests => new()
    {
        {
            "truncated header",
            RawIpcClient.RequestHeader(
                IpcConstants.ProtocolVersion,
                TestMessageTypes.Echo,
                payloadLength: 0)[..5]
        },
        {
            "declared payload ends early",
            [
                ..RawIpcClient.RequestHeader(
                    IpcConstants.ProtocolVersion,
                    TestMessageTypes.Echo,
                    payloadLength: 4),
                (byte)0xA5,
            ]
        },
    };

    [Theory(Timeout = IpcTestHost.TimeoutMilliseconds)]
    [MemberData(nameof(ServerRejectedRequests))]
    public async Task Invalid_headers_are_rejected_before_dispatch_and_the_listener_accepts_a_valid_request(
        string caseName,
        byte[] request)
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        var state = new ImmediateEchoHandlerState();
        await using var host = await IpcTestHostFactory.StartAsync<ImmediateEchoHandler>(
            configureServices: services => services.AddSingleton(state),
            cancellationToken: testCancellation);

        await RawIpcClient.SendRequestExpectingServerDisconnectAsync(
            host.PipeName,
            request,
            testCancellation);

        Assert.True(
            state.InvocationCount == 0,
            $"{caseName} unexpectedly dispatched a handler.");

        EchoResponse response = await host.Client.SendAsync<EchoRequest, EchoResponse>(
            new EchoRequest(23, "after-malformed"),
            testCancellation);

        Assert.Equal(new EchoResponse(23, "after-malformed", 23), response);
        Assert.Equal(1, state.InvocationCount);
    }

    [Theory(Timeout = IpcTestHost.TimeoutMilliseconds)]
    [MemberData(nameof(IncompleteRequests))]
    public async Task Client_disconnect_mid_frame_does_not_dispatch_and_the_listener_accepts_a_valid_request(
        string caseName,
        byte[] request)
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        var state = new ImmediateEchoHandlerState();
        await using var host = await IpcTestHostFactory.StartAsync<ImmediateEchoHandler>(
            configureServices: services => services.AddSingleton(state),
            cancellationToken: testCancellation);

        await RawIpcClient.SendPartialRequestAndDisconnectAsync(
            host.PipeName,
            request,
            testCancellation);

        Assert.True(
            state.InvocationCount == 0,
            $"{caseName} unexpectedly dispatched a handler.");

        EchoResponse response = await host.Client.SendAsync<EchoRequest, EchoResponse>(
            new EchoRequest(24, "after-disconnect"),
            testCancellation);

        Assert.Equal(new EchoResponse(24, "after-disconnect", 24), response);
        Assert.Equal(1, state.InvocationCount);
    }
}
