using System.Buffers;
using System.Buffers.Binary;
using System.Text.Json;

namespace FluentBitwarden.BrowseProxy.NativeMessaging;

internal sealed class NativeMessagingTransport(Stream input, Stream output) : INativeMessagingTransport
{
    private const int MaxInputBytes = 16 * 1024 * 1024;
    private const int MaxOutputBytes = 1024 * 1024;

    public async Task<BrowserNativeRequestEnvelope?> ReadRequestAsync(CancellationToken cancellationToken)
    {
        var lengthBuffer = new byte[4];
        int bytesRead = await input.ReadAsync(lengthBuffer.AsMemory(0, 1), cancellationToken);
        if (bytesRead == 0)
            return null;

        await input.ReadExactlyAsync(lengthBuffer.AsMemory(1), cancellationToken);

        uint length = BinaryPrimitives.ReadUInt32LittleEndian(lengthBuffer);
        if (length is 0 or > MaxInputBytes)
        {
            throw new InvalidDataException($"Invalid native message length: {length}.");
        }

        var messageBuffer = new byte[(int)length];
        await input.ReadExactlyAsync(messageBuffer, cancellationToken);

        return JsonSerializer.Deserialize<BrowserNativeRequestEnvelope>(
                   messageBuffer,
                   BrowseProxyJsonContext.ConfiguredDefault.BrowserNativeRequestEnvelope) ??
               throw new JsonException("The native message envelope cannot be null.");
    }

    public async Task WriteResponseAsync<T>(
        string requestId,
        T payload,
        CancellationToken cancellationToken)
    {
        var jsonTypeInfo = BrowseProxyJsonContext.ConfiguredDefault.GetRequiredTypeInfo<T>();
        var bufferWriter = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(bufferWriter))
        {
            writer.WriteStartObject();
            writer.WriteString("requestId", requestId);

            writer.WritePropertyName("payload");
            JsonSerializer.Serialize(writer, payload, jsonTypeInfo);

            writer.WriteEndObject();
        }

        if (bufferWriter.WrittenCount > MaxOutputBytes)
        {
            throw new InvalidDataException(
                $"Native response size {bufferWriter.WrittenCount} exceeds the {MaxOutputBytes}-byte browser limit.");
        }

        var lengthBuffer = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(lengthBuffer, (uint)bufferWriter.WrittenCount);

        await output.WriteAsync(lengthBuffer, cancellationToken);
        await output.WriteAsync(bufferWriter.WrittenMemory, cancellationToken);
        await output.FlushAsync(cancellationToken);
    }
}