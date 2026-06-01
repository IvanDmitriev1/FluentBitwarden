using System.Buffers.Binary;
using FluentBitwarden.AppHost.Modules.SshAgent.Models;

namespace FluentBitwarden.AppHost.Modules.SshAgent.Internal;

internal static class SshAgentProtocolReader
{
    public static bool TryReadLength(Stream stream, out int contentLength)
    {
        contentLength = 0;

        Span<byte> buffer = stackalloc byte[4];
        int read = stream.ReadAtLeast(buffer, buffer.Length, false);
        if (read < buffer.Length)
        {
            return false;
        }

        uint packetLength = BinaryPrimitives.ReadUInt32BigEndian(buffer);
        if (packetLength == 0)
        {
            return false;
        }

        contentLength = (int)packetLength;
        return true;
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