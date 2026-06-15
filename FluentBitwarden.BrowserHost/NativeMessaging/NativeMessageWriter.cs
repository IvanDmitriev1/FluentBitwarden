using FluentBitwarden.BrowserHost.Dispatching;
using System.Buffers.Binary;
using System.Text.Json;

namespace FluentBitwarden.BrowserHost.NativeMessaging;

internal sealed class NativeMessageWriter(Stream output)
{
    public async ValueTask WriteAsync(
        BrowserResponseEnvelope response,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(
            response,
            BrowserHostJsonContext.Default.BrowserResponseEnvelope);

        Span<byte> lengthPrefix = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(lengthPrefix, (uint)payload.Length);

        await output.WriteAsync(lengthPrefix.ToArray(), cancellationToken);
        await output.WriteAsync(payload, cancellationToken);
        await output.FlushAsync(cancellationToken);
    }
}
