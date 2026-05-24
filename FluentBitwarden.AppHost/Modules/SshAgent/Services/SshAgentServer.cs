using CommunityToolkit.HighPerformance.Buffers;
using FluentBitwarden.Application.Diagnostics;
using FluentBitwarden.Modules.SshAgent.Abstractions;
using FluentBitwarden.Modules.SshAgent.Internal;
using FluentBitwarden.Modules.SshAgent.Models;
using System.IO.Pipes;
using System.Linq;
using System.Text;

namespace FluentBitwarden.Modules.SshAgent.Services;

[Fody.ConfigureAwait(false)]
internal sealed class SshAgentServer(ISshKeyProvider sshKeyProvider) : ISshAgentServer, IDisposable
{
    private const string PipeName = "openssh-ssh-agent";
    public const int MaxPacketLength = 512 * 1024;

    private readonly CancellationTokenSource _cts = new();

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    public async Task RunAsync()
    {
        var token = _cts.Token;

        while (!token.IsCancellationRequested)
        {
            await using var server = new NamedPipeServerStream(
                PipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly,
                inBufferSize: 64 * 1024,
                outBufferSize: 64 * 1024);

            try
            {
                await server.WaitForConnectionAsync(token);
                await HandleClientAsync(server, token);
            }
            catch (Exception e) when (e is TaskCanceledException or OperationCanceledException or EndOfStreamException)
            {
                await SshAgentProtocolWriter.WriteFailureAsync(server, token);
            }
            catch (Exception e)
            {
                await SshAgentProtocolWriter.WriteFailureAsync(server, token);
                UnhandledExceptionLogger.WriteException(e);
            }
        }
    }

    private async Task HandleClientAsync(Stream stream, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && 
               SshAgentProtocolReader.TryReadLength(stream, out int length) && 
               length < MaxPacketLength)
        {
            using var bufferOwner = MemoryOwner<byte>.Allocate(length);
            await stream.ReadExactlyAsync(bufferOwner.Memory, ct);

            if (!SshAgentProtocolReader.TryReadPacket(bufferOwner.Memory, out var packet))
                return;

            Task task = packet.Message switch
            {
                SshAgentMessageRequests.RequestIdentities => HandleRequestIdentitiesAsync(stream, ct),
                SshAgentMessageRequests.SignRequest => HandleSignRequestAsync(stream, packet, ct),
                SshAgentMessageRequests.AgenticExtension => HandleExtensionRequest(stream, packet, ct),
                _ => SshAgentProtocolWriter.WriteFailureAsync(stream, ct)
            };

            await task;
        }
    }

    private async Task HandleRequestIdentitiesAsync(Stream stream, CancellationToken ct)
    {
        var identities = sshKeyProvider.ListIdentities();
        int identitiesLength = identities.Sum(static i =>
            4 + i.PublicKey.Length +
            4 + Encoding.UTF8.GetByteCount(i.Comment));

        int payloadLength =
            1 + // SSH_AGENT_IDENTITIES_ANSWER
            4 + // number of keys
            identitiesLength;

        using var writer = new SshAgentProtocolWriter(payloadLength, SshAgentMessageReplies.IdentitiesAnswer);
        writer.WriteUInt32(identities.Count);

        foreach (var identity in identities)
        {
            writer.WriteString(identity.PublicKey);
            writer.WriteString(identity.Comment);
        }

        await writer.WriteToAsync(stream, ct);
    }

    private async Task HandleExtensionRequest(Stream stream, SshAgentPacket packet, CancellationToken ct)
    {
        var request = SshAgentExtensionRequest.Parse(packet.Payload);
        if (!request.Payload.Span.SequenceEqual(SshAgentExtensionNames.Query))
        {
            await SshAgentProtocolWriter.WriteFailureAsync(stream, ct);
            return;
        }

        int queryLength = SshAgentExtensionNames.Query.Length;

        int payloadLength =
            1 +             // SSH_AGENT_EXTENSION_RESPONSE
            4 + queryLength + // string "query"
            4;              // uint32 extension count = 0

        using var writer = new SshAgentProtocolWriter(
            payloadLength,
            SshAgentMessageReplies.ExtensionResponse);

        writer.WriteString(SshAgentExtensionNames.Query);
        writer.WriteUInt32(0);

        await writer.WriteToAsync(stream, ct);
    }

    private async Task HandleSignRequestAsync(Stream stream, SshAgentPacket packet, CancellationToken ct)
    {
        var request = SshSignRequest.Parse(packet.Payload);
        if (!request.Flags.HasSupportedSignatures())
        {
            await SshAgentProtocolWriter.WriteFailureAsync(stream, ct);
            return;
        }

        var result = await sshKeyProvider.SignAsync(request, ct);
        if (result == SshSignatureResult.Failed)
        {
            await SshAgentProtocolWriter.WriteFailureAsync(stream, ct);
            return;
        }

        int algorithmLength = Encoding.ASCII.GetByteCount(result.AlgorithmName);
        int signatureLength = result.Signature.Length;

        int signatureBlobLength =
            4 + algorithmLength +
            4 + signatureLength;

        int payloadLength =
            1 + // SSH_AGENT_SIGN_RESPONSE
            4 + // outer string length
            signatureBlobLength;

        using var writer = new SshAgentProtocolWriter(
            payloadLength,
            SshAgentMessageReplies.SignResponse);

        writer.WriteUInt32(signatureBlobLength);
        writer.WriteString(result.AlgorithmName);
        writer.WriteString(result.Signature);

        await writer.WriteToAsync(stream, ct);
    }
}