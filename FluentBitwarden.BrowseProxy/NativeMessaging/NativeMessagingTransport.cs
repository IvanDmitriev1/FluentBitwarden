using CommunityToolkit.HighPerformance.Buffers;
using System.Buffers.Binary;
using System.Text.Json;

namespace FluentBitwarden.BrowseProxy.NativeMessaging;

internal sealed class NativeMessagingTransport(Stream input, Stream output) : INativeMessagingTransport
{
    private const int MaxInputBytes = 16 * 1024 * 1024;

    public async Task<BrowserNativeRequestEnvelope?> ReadRequestAsync(CancellationToken cancellationToken)
    {
        var lengthBuffer = new byte[4];
        await input.ReadExactlyAsync(lengthBuffer, cancellationToken);

        uint length = BinaryPrimitives.ReadUInt32LittleEndian(lengthBuffer);
        if (length is 0 or > MaxInputBytes)
        {
            throw new InvalidDataException($"Invalid native message length: {length}.");
        }

        using var messageBuffer = MemoryOwner<byte>.Allocate((int)length);
        await input.ReadExactlyAsync(messageBuffer.Memory, cancellationToken);

        return JsonSerializer.Deserialize<BrowserNativeRequestEnvelope>(messageBuffer.Span,
            BrowseProxyJsonContext.ConfiguredDefault.BrowserNativeRequestEnvelope);
    }

    public async Task WriteResponseAsync<T>(
        string requestId,
        T payload,
        CancellationToken cancellationToken)
    {
        var jsonTypeInfo = BrowseProxyJsonContext.ConfiguredDefault.GetRequiredTypeInfo<T>();
        using var bufferWriter = new ArrayPoolBufferWriter<byte>();

        await using (var writer = new Utf8JsonWriter(bufferWriter))
        {
            writer.WriteStartObject();
            writer.WriteString("requestId", requestId);

            writer.WritePropertyName("payload");
            JsonSerializer.Serialize(writer, payload, jsonTypeInfo);

            writer.WriteEndObject();
        }

        var lengthBuffer = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(lengthBuffer, (uint)bufferWriter.WrittenCount);

        await output.WriteAsync(lengthBuffer, cancellationToken);
        await output.WriteAsync(bufferWriter.WrittenMemory, cancellationToken);
        await output.FlushAsync(cancellationToken);
    }
}
