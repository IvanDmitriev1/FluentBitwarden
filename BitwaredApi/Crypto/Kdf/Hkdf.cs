using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.HighPerformance.Buffers;

namespace BitwaredApi.Crypto.Kdf;

internal static class Hkdf
{
    public static void Expand(ReadOnlySpan<byte> inputKeyMaterial, ReadOnlySpan<char> info, Span<byte> destination)
    {
        int infoByteCount = Encoding.UTF8.GetByteCount(info);
        Span<byte> infoBytes = stackalloc byte[infoByteCount];

        Encoding.UTF8.GetBytes(info, infoBytes);
        HKDF.Expand(HashAlgorithmName.SHA256, inputKeyMaterial, destination, infoBytes);
    }
}
