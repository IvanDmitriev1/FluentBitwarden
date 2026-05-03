using FluentBitwarden.Modules.SshAgent.Models;
using System.Buffers.Binary;

namespace FluentBitwarden.Modules.SshAgent.Internal;

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
        if (!TryGetMessage(rawType, out SshAgentMessage message)) 
            return false;

        packet = new SshAgentPacket(message, frame[1..]);
        return true;
    }

    private static bool TryGetMessage(byte value, out SshAgentMessage message)
    {
        switch (value)
        {
            case (byte)SshAgentMessage.Failure:
            case (byte)SshAgentMessage.Success:
            case (byte)SshAgentMessage.IdentitiesAnswer:
            case (byte)SshAgentMessage.SignResponse:
            case (byte)SshAgentMessage.ExtensionFailure:
            case (byte)SshAgentMessage.ExtensionResponse:

            case (byte)SshAgentMessage.RequestIdentities:
            case (byte)SshAgentMessage.SignRequest:
            case (byte)SshAgentMessage.Lock:
            case (byte)SshAgentMessage.Unlock:
            case (byte)SshAgentMessage.Extension:
                message = (SshAgentMessage)value;
                return true;

            default:
                message = default;
                return false;
        }
    }
}