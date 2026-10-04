using System.Buffers.Binary;
using FluentBitwarden.Platform.Ipc.Transport;

namespace FluentBitwarden.Platform.Tests.Ipc;

public class IpcRpcResponseHeaderTests
{
    [Theory]
    [InlineData((byte)2, 0)]
    [InlineData((byte)1, -1)]
    public async Task Invalid_success_flags_or_negative_payloads_are_rejected(
        byte successFlag,
        int payloadLength)
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        byte[] header = new byte[7];
        BinaryPrimitives.WriteUInt16LittleEndian(
            header.AsSpan(0, sizeof(ushort)),
            IpcConstants.ProtocolVersion);
        header[2] = successFlag;
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(3, sizeof(int)), payloadLength);

        await Assert.ThrowsAsync<InvalidDataException>(async () =>
            await IpcRpcResponseHeader.ReadAsync(new MemoryStream(header), testCancellation));
    }

    [Fact]
    public async Task Failure_header_can_carry_a_payload()
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        byte[] header = new byte[7];
        BinaryPrimitives.WriteUInt16LittleEndian(
            header.AsSpan(0, sizeof(ushort)),
            IpcConstants.ProtocolVersion);
        header[2] = 0;
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(3, sizeof(int)), 1);

        Assert.Equal(
            new IpcRpcResponseHeader(IsSuccessful: false, PayloadLength: 1),
            await IpcRpcResponseHeader.ReadAsync(new MemoryStream(header), testCancellation));
    }

    [Fact]
    public async Task Failure_header_writer_preserves_the_payload_length()
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        using var stream = new MemoryStream();
        await new IpcRpcResponseHeader(IsSuccessful: false, PayloadLength: 1).WriteAsync(
            stream,
            testCancellation);

        Assert.Equal(
            new IpcRpcResponseHeader(IsSuccessful: false, PayloadLength: 1),
            await IpcRpcResponseHeader.ReadAsync(new MemoryStream(stream.ToArray()), testCancellation));
    }

    [Fact]
    public async Task Failure_header_is_seven_bytes_and_has_an_empty_payload()
    {
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        using var stream = new MemoryStream();
        await new IpcRpcResponseHeader(IsSuccessful: false, PayloadLength: 0).WriteAsync(
            stream,
            testCancellation);

        Assert.Equal(7, stream.Length);
        Assert.Equal(
            new IpcRpcResponseHeader(IsSuccessful: false, PayloadLength: 0),
            await IpcRpcResponseHeader.ReadAsync(
                new MemoryStream(stream.ToArray()),
                testCancellation));
    }

}
