using System.Buffers.Binary;
using System.Text;
using FluentBitwarden.AppHost.Integrations.SshAgent.Abstractions;
using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

namespace FluentBitwarden.AppHost.Integrations.SshAgent.Services;

internal sealed class SshAgentMessagesHandler(ISshKeyProvider keyProvider) : ISshAgentMessagesHandler
{
    public async Task<SshAgentMessage> HandleAsync(SshAgentMessage message, CancellationToken cancellationToken)
    {
        SshAgentMessage response = message.Type switch
        {
            SshAgentMessageType.RequestIdentities => HandleRequestIdentities(),
            SshAgentMessageType.SignRequest => await HandleSignRequestAsync(message.Payload, cancellationToken),
            SshAgentMessageType.Extension => HandleExtensionRequest(message.Payload),
            _ => SshAgentMessage.Failure
        };

        return response;
    }

    private SshAgentMessage HandleRequestIdentities()
    {
        var identities = keyProvider.GetIdentities();
        long payloadLength = sizeof(uint) + identities.Sum(static i => i.BlobLength);

        if (payloadLength + 1 > SshAgentMessage.MaxLength)
        {
            return SshAgentMessage.Failure;
        }

        byte[] payload = new byte[(int)payloadLength];
        int offset = 0;

        WriteUInt32(payload, ref offset, (uint)identities.Count);

        foreach (var identity in identities)
        {
            WriteString(payload, ref offset, identity.PublicKeyBlob.Span);
            WriteUtf8String(payload, ref offset, identity.Comment);
        }

        return new SshAgentMessage(SshAgentMessageType.IdentitiesAnswer, payload);
    }

    private async Task<SshAgentMessage> HandleSignRequestAsync(ReadOnlyMemory<byte> messagePayload, CancellationToken cancellationToken)
    {
        if (!SshAgentSignRequest.TryParse(messagePayload, out var signRequest) ||
            signRequest.Flags is not (
                SshAgentSignatureFlags.None or
                SshAgentSignatureFlags.RsaSha2256 or
                SshAgentSignatureFlags.RsaSha2512))
        {
            return SshAgentMessage.Failure;
        }

        var signature = await keyProvider.GetSignatureAsync(signRequest, cancellationToken);
        if (signature is null)
            return SshAgentMessage.Failure;

        long payloadLength = sizeof(uint) + signature.BlobLength;
        if (payloadLength + 1 > SshAgentMessage.MaxLength)
            return SshAgentMessage.Failure;

        byte[] payload = new byte[(int)payloadLength];
        int offset = 0;

        // Outer SSH string length.
        WriteUInt32(payload, ref offset, (uint)signature.BlobLength);

        // Inner SSH signature blob.
        WriteUtf8String(payload, ref offset, signature.Algorithm);

        WriteString(payload, ref offset, signature.Value);

        return new SshAgentMessage(SshAgentMessageType.SignResponse, payload);
    }
    private SshAgentMessage HandleExtensionRequest(ReadOnlyMemory<byte> messagePayload)
    {
        if (!SshAgentExtensionRequest.TryParse(messagePayload, out var request) ||
            !request.ExtensionName.Span.SequenceEqual("query"u8) ||
            request.Data.IsEmpty)
        {
            return new SshAgentMessage(SshAgentMessageType.ExtensionFailure, Array.Empty<byte>());
        }

        //TODO implement later
        return new SshAgentMessage(SshAgentMessageType.ExtensionFailure, Array.Empty<byte>());
    }

    private static void WriteUInt32(Span<byte> buffer, ref int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, sizeof(uint)), value);
        offset += sizeof(uint);
    }

    private static void WriteString(Span<byte> buffer, ref int offset, ReadOnlySpan<byte> value)
    {
        WriteUInt32(buffer, ref offset, (uint)value.Length);

        value.CopyTo(buffer.Slice(offset, value.Length));
        offset += value.Length;
    }

    private static void WriteUtf8String(Span<byte> buffer, ref int offset, string value)
    {
        int lengthOffset = offset;
        offset += sizeof(uint);

        int bytesWritten = Encoding.UTF8.GetBytes(value.AsSpan(), buffer[offset..]);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(lengthOffset, sizeof(uint)), (uint)bytesWritten);
        offset += bytesWritten;
    }
}
