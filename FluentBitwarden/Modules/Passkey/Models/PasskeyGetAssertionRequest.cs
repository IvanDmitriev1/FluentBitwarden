using System.Buffers;
using FluentBitwarden.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Infrastructure.Ipc.Internal;

namespace FluentBitwarden.Modules.Passkey.Models;

internal readonly record struct PasskeyGetAssertionRequest(
    string RpId,
    byte[] RpIdHash,
    byte[] ClientDataHash) : IPipeRequest<PasskeyGetAssertionRequest>
{
    public static ushort MessageType => 2;

    private const ushort SchemaVersion = 1;
    private const int Sha256HashLength = 32;

    public static PasskeyGetAssertionRequest ReadPayload(ReadOnlySpan<byte> payload)
    {
        var reader = new IpcPayloadReader(payload);
        reader.ReadSchemaVersion(SchemaVersion, "passkey assertion request");

        var rpId = reader.ReadString();
        var rpIdHash = reader.ReadByteArray();
        var clientDataHash = reader.ReadByteArray();
        reader.EnsureConsumed();

        ValidateHashLength(rpIdHash, nameof(RpIdHash));
        ValidateHashLength(clientDataHash, nameof(ClientDataHash));

        return new PasskeyGetAssertionRequest(rpId, rpIdHash, clientDataHash);
    }

    public void WritePayload(IBufferWriter<byte> writer)
    {
        ValidateHashLength(RpIdHash, nameof(RpIdHash));
        ValidateHashLength(ClientDataHash, nameof(ClientDataHash));

        var payloadWriter = new IpcPayloadWriter(writer);
        payloadWriter.WriteSchemaVersion(SchemaVersion);
        payloadWriter.WriteString(RpId);
        payloadWriter.WriteByteArray(RpIdHash);
        payloadWriter.WriteByteArray(ClientDataHash);
    }

    private static void ValidateHashLength(byte[] value, string fieldName)
    {
        if (value.Length != Sha256HashLength)
        {
            throw new InvalidOperationException(
                $"{fieldName} must be {Sha256HashLength} bytes, got {value.Length}.");
        }
    }
}
