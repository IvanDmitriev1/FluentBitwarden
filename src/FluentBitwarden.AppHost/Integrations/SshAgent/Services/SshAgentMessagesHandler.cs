using FluentBitwarden.AppHost.Integrations.SshAgent.Abstractions;
using FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

namespace FluentBitwarden.AppHost.Integrations.SshAgent.Services;

internal sealed class SshAgentMessagesHandler(ISshKeyProvider keyProvider) : ISshAgentMessagesHandler
{
    public async Task<SshAgentMessage> HandleAsync(
        SshAgentMessage message,
        SshAgentConnectionState connectionState,
        CancellationToken cancellationToken)
    {
        SshAgentMessage response = message.Type switch
        {
            SshAgentMessageType.RequestIdentities => await HandleRequestIdentitiesAsync(cancellationToken),
            SshAgentMessageType.SignRequest => await HandleSignRequestAsync(message.Payload, connectionState, cancellationToken),
            SshAgentMessageType.Extension => HandleExtensionRequest(message.Payload, connectionState),
            _ => SshAgentMessage.Failure
        };

        return response;
    }

    private async Task<SshAgentMessage> HandleRequestIdentitiesAsync(CancellationToken cancellationToken)
    {
        var identities = await keyProvider.GetIdentitiesAsync(cancellationToken);
        long payloadLength = sizeof(uint) + identities.Sum(static i => i.BlobLength);

        if (payloadLength + 1 > SshAgentMessage.MaxLength)
        {
            return SshAgentMessage.Failure;
        }

        byte[] payload = new byte[(int)payloadLength];
        int offset = 0;

        SshAgentPayloadWriter.WriteUInt32(payload, ref offset, (uint)identities.Count);

        foreach (var identity in identities)
        {
            SshAgentPayloadWriter.WriteString(payload, ref offset, identity.PublicKeyBlob.Span);
            SshAgentPayloadWriter.WriteUtf8String(payload, ref offset, identity.Comment);
        }

        return new SshAgentMessage(SshAgentMessageType.IdentitiesAnswer, payload);
    }

    private async Task<SshAgentMessage> HandleSignRequestAsync(
        ReadOnlyMemory<byte> messagePayload,
        SshAgentConnectionState connectionState,
        CancellationToken cancellationToken)
    {
        if (!SshAgentSignRequest.TryParse(messagePayload, out var signRequest) ||
            signRequest.Flags is not (
                SshAgentSignatureFlags.None or
                SshAgentSignatureFlags.RsaSha2256 or
                SshAgentSignatureFlags.RsaSha2512))
        {
            return SshAgentMessage.Failure;
        }

        var signature = await keyProvider.GetSignatureAsync(
            signRequest,
            connectionState.SessionBinding,
            connectionState.IsForwardingConnection,
            cancellationToken);

        if (signature is null)
            return SshAgentMessage.Failure;

        long payloadLength = sizeof(uint) + signature.BlobLength;
        if (payloadLength + 1 > SshAgentMessage.MaxLength)
            return SshAgentMessage.Failure;

        byte[] payload = new byte[(int)payloadLength];
        int offset = 0;

        // Outer SSH string length.
        SshAgentPayloadWriter.WriteUInt32(payload, ref offset, (uint)signature.BlobLength);

        // Inner SSH signature blob.
        SshAgentPayloadWriter.WriteUtf8String(payload, ref offset, signature.Algorithm);

        SshAgentPayloadWriter.WriteString(payload, ref offset, signature.Value);

        return new SshAgentMessage(SshAgentMessageType.SignResponse, payload);
    }
    private SshAgentMessage HandleExtensionRequest(
        ReadOnlyMemory<byte> messagePayload,
        SshAgentConnectionState connectionState)
    {
        if (!SshAgentExtensionRequest.TryParse(messagePayload, out var request))
        {
            return new SshAgentMessage(SshAgentMessageType.ExtensionFailure, Array.Empty<byte>());
        }

        if (request.ExtensionName.Span.SequenceEqual("session-bind@openssh.com"u8))
        {
            if (SshAgentSessionBinding.IsForwardingBinding(request.Data))
            {
                connectionState.MarkForwardingConnection();
                return new SshAgentMessage(SshAgentMessageType.ExtensionFailure, Array.Empty<byte>());
            }

            return SshAgentSessionBinding.TryParseAndVerify(request.Data, out var binding) && connectionState.TryBind(binding)
                ? new SshAgentMessage(SshAgentMessageType.Success, Array.Empty<byte>())
                : new SshAgentMessage(SshAgentMessageType.ExtensionFailure, Array.Empty<byte>());
        }

        if (!request.ExtensionName.Span.SequenceEqual("query"u8) || request.Data.IsEmpty)
            return new SshAgentMessage(SshAgentMessageType.ExtensionFailure, Array.Empty<byte>());

        //TODO implement later
        return new SshAgentMessage(SshAgentMessageType.ExtensionFailure, Array.Empty<byte>());
    }
}
