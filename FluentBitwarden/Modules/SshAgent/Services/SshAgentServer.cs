using CommunityToolkit.HighPerformance.Buffers;
using FluentBitwarden.Modules.SshAgent.Abstractions;
using FluentBitwarden.Modules.SshAgent.Internal;
using FluentBitwarden.Modules.SshAgent.Models;
using System.Diagnostics;
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
            try
            {
                await using var server = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly,
                    inBufferSize: 64 * 1024,
                    outBufferSize: 64 * 1024);

                await server.WaitForConnectionAsync(token);
                await HandleClientAsync(server, token);
            }
            catch (Exception e) when (e is TaskCanceledException or OperationCanceledException or EndOfStreamException or IOException)
            {
                break;
            }
            catch (Exception e)
            {
                //
            }
        }
    }

    private async Task HandleClientAsync(Stream stream, CancellationToken ct)
    {
        if (!SshAgentProtocolReader.TryReadLength(stream, out int length))
            return;

        if (length > MaxPacketLength)
        {
            Debug.WriteLine("Packet length exceeds maximum allowed length.");
            return;
        }

        using var bufferOwner = MemoryOwner<byte>.Allocate(length);
        await stream.ReadExactlyAsync(bufferOwner.Memory, ct);

        if (!SshAgentProtocolReader.TryReadPacket(bufferOwner.Memory, out var packet))
            return;

        Task task = packet.Message switch
        {
            SshAgentMessage.RequestIdentities => HandleRequestIdentitiesAsync(stream, ct),
            _ => SshAgentProtocolWriter.WriteFailureAsync(stream, ct)
        };

        await task;
    }

    private async Task HandleRequestIdentitiesAsync(
        Stream stream,
        CancellationToken ct)
    {
        var identities = sshKeyProvider.ListIdentities();
        int identitiesLength = identities.Sum(static i =>
            4 + i.PublicKey.KeyBlob.Length +
            4 + Encoding.UTF8.GetByteCount(i.Comment));

        int payloadLength =
            1 + // SSH_AGENT_IDENTITIES_ANSWER
            4 + // number of keys
            identitiesLength;

        using var writer = new SshAgentProtocolWriter(payloadLength, SshAgentMessage.IdentitiesAnswer);
        writer.WriteUInt32(identities.Count);

        foreach (var identity in identities)
        {
            writer.WriteString(identity.PublicKey.KeyBlob);
            writer.WriteString(identity.Comment);
        }

        await writer.WriteToAsync(stream, ct);
    }
}