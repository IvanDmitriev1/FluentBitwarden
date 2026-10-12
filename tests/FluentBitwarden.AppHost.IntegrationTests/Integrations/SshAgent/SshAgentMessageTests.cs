using System.Buffers.Binary;
using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

namespace FluentBitwarden.AppHost.IntegrationTests.Integrations.SshAgent;

public sealed class SshAgentMessageTests
{
    [Fact]
    public async Task WriteMessage_writes_the_independently_encoded_frame()
    {
        var message = new SshAgentMessage(SshAgentMessageType.SignRequest, new byte[] { 0x10, 0x20 });
        using var stream = new MemoryStream();

        await message.WriteMessageAsync(stream, CancellationToken.None);

        Assert.Equal(new byte[] { 0, 0, 0, 3, 13, 0x10, 0x20 }, stream.ToArray());
    }

    [Fact]
    public async Task ReadMessage_reads_a_frame_across_partial_stream_reads()
    {
        byte[] frame = [0, 0, 0, 4, 11, 0x10, 0x20, 0x30];
        using var stream = new ShortReadStream(frame, 2);

        SshAgentMessage message = await SshAgentMessage.ReadAsync(stream, CancellationToken.None);

        Assert.Equal(SshAgentMessageType.RequestIdentities, message.Type);
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30 }, message.Payload.ToArray());
    }

    [Fact]
    public async Task ReadMessage_rejects_a_zero_length()
    {
        byte[] header = SshAgentTestData.EncodeUInt32(0);
        using var stream = new MemoryStream(header);

        SshAgentMessage message = await SshAgentMessage.ReadAsync(stream, CancellationToken.None);

        Assert.Equal(SshAgentMessage.Failure, message);
    }

    [Fact]
    public async Task ReadMessage_rejects_an_oversized_length_without_reading_its_body()
    {
        byte[] frame = new byte[sizeof(uint) + SshAgentMessage.MaxLength + 1];
        BinaryPrimitives.WriteUInt32BigEndian(frame, SshAgentMessage.MaxLength + 1);
        frame[sizeof(uint)] = (byte)SshAgentMessageType.Success;
        using var stream = new MemoryStream(frame);

        SshAgentMessage message = await SshAgentMessage.ReadAsync(stream, CancellationToken.None);

        Assert.Equal(SshAgentMessage.Failure, message);
        Assert.Equal(sizeof(uint), stream.Position);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(7)]
    public async Task ReadMessage_rejects_truncated_header_or_body(int frameLength)
    {
        byte[] frame = frameLength == 2
            ? [0, 0]
            : [0, 0, 0, 5, 11, 0x10, 0x20];
        using var stream = new ShortReadStream(frame, 2);

        SshAgentMessage message = await SshAgentMessage.ReadAsync(stream, CancellationToken.None);

        Assert.Equal(SshAgentMessage.Failure, message);
    }

    [Fact]
    public async Task ReadMessage_accepts_the_maximum_body_length()
    {
        byte[] frame = new byte[sizeof(uint) + SshAgentMessage.MaxLength];
        BinaryPrimitives.WriteUInt32BigEndian(frame, SshAgentMessage.MaxLength);
        frame[sizeof(uint)] = (byte)SshAgentMessageType.Success;
        frame[^1] = 0xA5;
        using var stream = new MemoryStream(frame);

        SshAgentMessage message = await SshAgentMessage.ReadAsync(stream, CancellationToken.None);

        Assert.Equal(SshAgentMessageType.Success, message.Type);
        Assert.Equal(SshAgentMessage.MaxLength - 1, message.Payload.Length);
        Assert.Equal((byte)0xA5, message.Payload.Span[^1]);
    }

    [Fact]
    public async Task WriteMessage_accepts_the_maximum_and_rejects_one_byte_over()
    {
        byte[] maximumPayload = new byte[SshAgentMessage.MaxLength - 1];
        maximumPayload[^1] = 0xA5;
        using var stream = new MemoryStream();

        await new SshAgentMessage(SshAgentMessageType.Success, maximumPayload)
            .WriteMessageAsync(stream, CancellationToken.None);

        byte[] frame = stream.ToArray();
        Assert.Equal((uint)SshAgentMessage.MaxLength, BinaryPrimitives.ReadUInt32BigEndian(frame));
        Assert.Equal((byte)SshAgentMessageType.Success, frame[sizeof(uint)]);
        Assert.Equal((byte)0xA5, frame[^1]);

        var oversized = new SshAgentMessage(
            SshAgentMessageType.Success,
            new byte[SshAgentMessage.MaxLength]);
        await Assert.ThrowsAsync<InvalidDataException>(
            () => oversized.WriteMessageAsync(Stream.Null, CancellationToken.None).AsTask());
    }

    private sealed class ShortReadStream(byte[] data, int maxRead) : MemoryStream(data)
    {
        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default) =>
            base.ReadAsync(buffer[..Math.Min(buffer.Length, maxRead)], cancellationToken);
    }
}
