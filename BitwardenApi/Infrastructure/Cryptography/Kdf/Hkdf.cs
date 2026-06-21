using System.Security.Cryptography;
using System.Text;

namespace BitwardenApi.Infrastructure.Cryptography.Kdf;

internal static class Hkdf
{
    public static void Expand(ReadOnlySpan<byte> inputKeyMaterial, ReadOnlySpan<char> info, Span<byte> destination)
    {
        int infoByteCount = System.Text.Encoding.UTF8.GetByteCount(info);
        Span<byte> infoBytes = stackalloc byte[infoByteCount];

        System.Text.Encoding.UTF8.GetBytes(info, infoBytes);
        HKDF.Expand(HashAlgorithmName.SHA256, inputKeyMaterial, destination, infoBytes);
    }
}
