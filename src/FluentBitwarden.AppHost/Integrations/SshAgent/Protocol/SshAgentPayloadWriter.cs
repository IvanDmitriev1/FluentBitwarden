using System.Buffers.Binary;
using System.Text;

namespace FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

internal static class SshAgentPayloadWriter
{
    public static void WriteUInt32(Span<byte> buffer, ref int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, sizeof(uint)), value);
        offset += sizeof(uint);
    }

    public static void WriteString(Span<byte> buffer, ref int offset, ReadOnlySpan<byte> value)
    {
        WriteUInt32(buffer, ref offset, (uint)value.Length);

        value.CopyTo(buffer.Slice(offset, value.Length));
        offset += value.Length;
    }

    public static void WriteUtf8String(Span<byte> buffer, ref int offset, string value)
    {
        int lengthOffset = offset;
        offset += sizeof(uint);

        int bytesWritten = Encoding.UTF8.GetBytes(value.AsSpan(), buffer[offset..]);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(lengthOffset, sizeof(uint)), (uint)bytesWritten);
        offset += bytesWritten;
    }
}
