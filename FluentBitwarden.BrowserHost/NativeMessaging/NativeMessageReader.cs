using System.Buffers.Binary;
using System.Text;

namespace FluentBitwarden.BrowserHost.NativeMessaging;

internal sealed class NativeMessageReader(Stream input)
{
    public const int DefaultMaxMessageSize = 1024 * 1024;

    public async ValueTask<NativeMessageReadResult> ReadAsync(CancellationToken cancellationToken)
    {
        byte[] lengthPrefix = new byte[sizeof(uint)];
        var lengthBytesRead = await ReadSomeAsync(lengthPrefix, cancellationToken);

        if (lengthBytesRead == 0)
            return NativeMessageReadResult.EndOfStream;

        if (lengthBytesRead != lengthPrefix.Length)
        {
            throw new NativeMessageProtocolException(
                "invalid_request",
                "Unexpected end of stream while reading the native message length.");
        }

        var messageLength = BinaryPrimitives.ReadUInt32LittleEndian(lengthPrefix);
        if (messageLength > DefaultMaxMessageSize)
        {
            throw new NativeMessageProtocolException(
                "message_too_large",
                $"Native message length exceeds the {DefaultMaxMessageSize} byte limit.",
                canContinue: false);
        }

        if (messageLength == 0)
        {
            throw new NativeMessageProtocolException(
                "invalid_request",
                "Native message payload must not be empty.");
        }

        byte[] payload = new byte[(int)messageLength];
        var payloadBytesRead = await ReadSomeAsync(payload, cancellationToken);

        if (payloadBytesRead != payload.Length)
        {
            throw new NativeMessageProtocolException(
                "invalid_request",
                "Unexpected end of stream while reading the native message payload.");
        }

        return NativeMessageReadResult.Message(Encoding.UTF8.GetString(payload));
    }

    private async ValueTask<int> ReadSomeAsync(byte[] buffer, CancellationToken cancellationToken)
    {
        var totalBytesRead = 0;

        while (totalBytesRead < buffer.Length)
        {
            var bytesRead = await input.ReadAsync(
                buffer.AsMemory(totalBytesRead, buffer.Length - totalBytesRead),
                cancellationToken);

            if (bytesRead == 0)
                return totalBytesRead;

            totalBytesRead += bytesRead;
        }

        return totalBytesRead;
    }
}
