using FluentBitwarden.Platform.Ipc.Transport;

namespace FluentBitwarden.Platform.Tests.Ipc;

public class IpcRpcFailureResponseTests
{
    [Theory]
    [InlineData((byte)0)]
    [InlineData((byte)1)]
    [InlineData((byte)2)]
    public async Task Failure_payload_round_trips_each_defined_code(byte codeValue)
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        var code = (IpcRpcFailureCode)codeValue;
        byte[] payload = IpcWireProtocol.SerializeRpcFailureResponse(code);

        IpcRpcFailureResponse response = await IpcWireProtocol.ReadRpcFailureResponseAsync(
            new MemoryStream(payload),
            payload.Length,
            testCancellation);

        Assert.Equal(code, response.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("FF")]
    [InlineData("0000")]
    public async Task Malformed_failure_payload_is_rejected(string hexPayload)
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        byte[] payload = Convert.FromHexString(hexPayload);

        await Assert.ThrowsAsync<InvalidDataException>(async () =>
            await IpcWireProtocol.ReadRpcFailureResponseAsync(
                new MemoryStream(payload),
                payload.Length,
                testCancellation));
    }
}
