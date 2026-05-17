using System.Buffers;
using FluentBitwarden.Infrastructure.Ipc.Abstractions;
using FluentBitwarden.Infrastructure.Ipc.Internal;

namespace FluentBitwarden.Modules.Passkey.Models;

internal sealed class PasskeyAssertionResponse : IPipeMessage<PasskeyAssertionResponse>
{
    private const ushort SchemaVersion = 1;

    // Credential selected by the vault.
    public required byte[] CredentialId { get; init; }

    // User handle from the original passkey credential.
    public required byte[] UserId { get; init; }

    // Authenticator data bytes.
    public required byte[] AuthenticatorData { get; init; }

    // Signature over authenticatorData || clientDataHash.
    public required byte[] Signature { get; init; }

    public required string UserName { get; init; }
    public required string UserDisplayName { get; init; }

    public static PasskeyAssertionResponse ReadPayload(ReadOnlySpan<byte> payload)
    {
        var reader = new IpcPayloadReader(payload);
        reader.ReadSchemaVersion(SchemaVersion, "passkey assertion response");

        var response = new PasskeyAssertionResponse
        {
            CredentialId = reader.ReadByteArray(),
            UserId = reader.ReadByteArray(),
            AuthenticatorData = reader.ReadByteArray(),
            Signature = reader.ReadByteArray(),
            UserName = reader.ReadString(),
            UserDisplayName = reader.ReadString()
        };

        reader.EnsureConsumed();
        return response;
    }

    public void WritePayload(IBufferWriter<byte> writer)
    {
        var payloadWriter = new IpcPayloadWriter(writer);
        payloadWriter.WriteSchemaVersion(SchemaVersion);
        payloadWriter.WriteByteArray(CredentialId);
        payloadWriter.WriteByteArray(UserId);
        payloadWriter.WriteByteArray(AuthenticatorData);
        payloadWriter.WriteByteArray(Signature);
        payloadWriter.WriteString(UserName);
        payloadWriter.WriteString(UserDisplayName);
    }
}
