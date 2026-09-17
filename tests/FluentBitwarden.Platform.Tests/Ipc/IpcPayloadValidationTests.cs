using FluentBitwarden.Platform.Tests.Ipc.Infrastructure;

namespace FluentBitwarden.Platform.Tests.Ipc;

public class IpcPayloadValidationTests
{
    public static TheoryData<string, byte[], bool> MalformedRequests => new()
    {
        {
            "truncated header",
            RawIpcClient.RequestHeader(
                IpcConstants.ProtocolVersion,
                TestMessageTypes.Echo,
                payloadLength: 0)[..5],
            true
        },
        {
            "unsupported protocol version",
            RawIpcClient.RequestHeader(
                (ushort)(IpcConstants.ProtocolVersion + 1),
                TestMessageTypes.Echo,
                payloadLength: 0),
            false
        },
        {
            "negative payload length",
            RawIpcClient.RequestHeader(
                IpcConstants.ProtocolVersion,
                TestMessageTypes.Echo,
                payloadLength: -1),
            false
        },
        {
            "declared payload ends early",
            [
                ..RawIpcClient.RequestHeader(
                    IpcConstants.ProtocolVersion,
                    TestMessageTypes.Echo,
                    payloadLength: 4),
                (byte)0xA5,
            ],
            true
        },
        {
            "unknown message id",
            RawIpcClient.RequestHeader(
                IpcConstants.ProtocolVersion,
                ushort.MaxValue,
                payloadLength: 0),
            false
        },
    };

    [Theory(Timeout = 1000)]
    [MemberData(nameof(MalformedRequests))]
    public async Task Malformed_requests_fail_closed_and_the_same_listener_accepts_a_valid_request(
        string caseName,
        byte[] request,
        bool closeAfterWrite)
    {
        await using var host = await IpcTestHost.StartAsync<ImmediateEchoHandler>();
        var handler = host.Handler<ImmediateEchoHandler>();
        CancellationToken testCancellation = TestContext.Current.CancellationToken;

        Exception failure = await RawIpcClient.SendMalformedRequestAsync(
            host.PipeName,
            request,
            closeAfterWrite);

        Assert.True(
            failure is IOException or ObjectDisposedException,
            $"{caseName} produced an unexpected client-visible failure: {failure.GetType().FullName}");
        Assert.Equal(0, handler.InvocationCount);

        EchoResponse response = await host.Client.SendAsync<EchoRequest, EchoResponse>(
            new EchoRequest(23, "after-malformed"),
            testCancellation);

        Assert.Equal(new EchoResponse(23, "after-malformed", 23), response);
        Assert.Equal(1, handler.InvocationCount);
    }
}
