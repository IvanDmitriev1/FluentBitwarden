using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace FluentBitwarden.Infrastructure.Ipc.Internal;

internal readonly ref struct IpcPayloadWriter(IBufferWriter<byte> writer)
{
    public void WriteSchemaVersion(ushort schemaVersion)
    {
        WriteUInt16(schemaVersion);
    }

    private void WriteUInt16(ushort value)
    {
        Span<byte> bytes = writer.GetSpan(sizeof(ushort));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
        writer.Advance(sizeof(ushort));
    }

    private void WriteUInt32(uint value)
    {
        Span<byte> bytes = writer.GetSpan(sizeof(uint));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        writer.Advance(sizeof(uint));
    }

    public void WriteByteArray(ReadOnlySpan<byte> value)
    {
        WriteLength(value.Length);
        if (value.IsEmpty)
            return;

        Span<byte> bytes = writer.GetSpan(value.Length);
        value.CopyTo(bytes);
        writer.Advance(value.Length);
    }

    public void WriteByteArray(byte[] value)
    {
        WriteByteArray(value.AsSpan());
    }

    public void WriteString(string value)
    {
        int byteCount = Encoding.UTF8.GetByteCount(value);
        WriteLength(byteCount);
        if (byteCount == 0)
            return;

        Span<byte> bytes = writer.GetSpan(byteCount);
        int written = Encoding.UTF8.GetBytes(value.AsSpan(), bytes);
        writer.Advance(written);
    }

    private void WriteLength(int length)
    {
        if (length < 0)
            throw new InvalidOperationException($"Invalid IPC field length: {length} bytes.");

        WriteUInt32((uint)length);
    }
}
