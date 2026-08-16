using System.Buffers.Binary;
using FluentBitwarden.AppHost.Modules.SshAgent.Models;

namespace FluentBitwarden.AppHost.Modules.SshAgent.Internal;

internal static class SshAgentProtocolReader
{
    public static async Task<int> ReadLengthAsync(Stream stream, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[4];
        await stream.ReadExactlyAsync(buffer, cancellationToken);

        uint contentLength = BinaryPrimitives.ReadUInt32BigEndian(buffer);
        return (int)contentLength;
    }

    public static bool TryReadPacket(ReadOnlyMemory<byte> frame, out SshAgentPacket packet)
    {
        packet = default;

        if (frame.IsEmpty)
            return false;

        byte rawType = frame.Span[0];
        packet = new SshAgentPacket((SshAgentMessageRequests)rawType, frame[1..]);
        return true;
    }
}