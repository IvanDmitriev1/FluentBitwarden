using FluentBitwarden.Platform.Ipc.Transport;
using FluentBitwarden.Platform.Tests.Ipc.Infrastructure;

namespace FluentBitwarden.Platform.Tests.Ipc;

public class IpcMessageHeaderTests
{
    [Theory(Timeout = IpcTestHost.TimeoutMilliseconds)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(7)]
    public void Parse_rejects_incomplete_headers(int headerLength)
    {
        TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
        byte[] bytes = RawIpcClient.RequestHeader(
            IpcConstants.ProtocolVersion, TestMessageTypes.Echo, payloadLength: 0);
        Assert.Throws<InvalidDataException>(() => IpcMessageHeader.Parse(bytes.AsSpan(0, headerLength)));
    }

    [Fact(Timeout = IpcTestHost.TimeoutMilliseconds)]
    public void Parse_returns_message_type_and_payload_length()
    {
        TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
        byte[] bytes = RawIpcClient.RequestHeader(
            IpcConstants.ProtocolVersion, TestMessageTypes.Echo, payloadLength: 42);

        IpcMessageHeader header = IpcMessageHeader.Parse(bytes);

        Assert.Equal(TestMessageTypes.Echo, header.MessageType);
        Assert.Equal(42, header.PayloadLength);
        Assert.True(header.IsValid);
    }

    [Theory(Timeout = IpcTestHost.TimeoutMilliseconds)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(8)]
    public async Task Read_returns_an_invalid_header_only_when_the_frame_is_incomplete(int length)
    {
        byte[] bytes = RawIpcClient.RequestHeader(
            IpcConstants.ProtocolVersion, TestMessageTypes.Echo, payloadLength: 42);
        using var stream = new MemoryStream(bytes[..length]);
        byte[] buffer = new byte[IpcMessageHeader.HeaderSize];

        IpcMessageHeader header = await IpcMessageHeader.ReadAsync(
            stream, buffer, TestContext.Current.CancellationToken);

        Assert.Equal(length == bytes.Length, header.IsValid);
        Assert.Equal(length == bytes.Length ? new IpcMessageHeader(TestMessageTypes.Echo, 42) : default, header);
    }
}
