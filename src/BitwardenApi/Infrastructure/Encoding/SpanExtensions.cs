namespace BitwardenApi.Infrastructure.Encoding;

public static class SpanExtensions
{
    public static int RemoveAsciiWhitespaceInPlace(this Span<byte> buffer)
    {
        int write = 0;

        for (int read = 0; read < buffer.Length; read++)
        {
            byte b = buffer[read];

            if (b is not ((byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n'))
            {
                buffer[write++] = b;
            }
        }

        return write;
    }
}