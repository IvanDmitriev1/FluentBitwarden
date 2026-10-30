using System.Buffers.Binary;

namespace BitwardenApi.Tests.Infrastructure;

internal static class CryptoTestFixtures
{
    public const string LegacyAesIvBase64 = "AAECAwQFBgcICQoLDA0ODw==";
    public const string LegacyAesCiphertextBase64 = "W34jgMGBK7Fatz0Y6BG5PXiDjJnTOewWO+Ie4tXoxLY=";

    public const string AuthenticatedIvBase64 = "AAECAwQFBgcICQoLDA0ODw==";
    public const string AuthenticatedCiphertextBase64 = "URUSTubWO/mgdJPbxF260L2ZR++Nqev9R2ivTxISOOs=";
    public const string AuthenticatedMacBase64 = "9SVSKKsSfeuCZA/eHiHEWz2vSxZR3+x5ubAyGjy284Y=";
    public const string AuthenticatedEncString =
        "2.AAECAwQFBgcICQoLDA0ODw==|URUSTubWO/mgdJPbxF260L2ZR++Nqev9R2ivTxISOOs=|9SVSKKsSfeuCZA/eHiHEWz2vSxZR3+x5ubAyGjy284Y=";

    public static byte[] AesKey() => [.. Enumerable.Range(0, 32).Select(static value => (byte)value)];

    public static byte[] CombinedKey() => [.. Enumerable.Range(0, 64).Select(static value => (byte)value)];

    public static byte[] Pack(
        EncryptionType type,
        ReadOnlySpan<byte> iv,
        ReadOnlySpan<byte> data,
        ReadOnlySpan<byte> mac)
    {
        byte[] bytes = new byte[13 + iv.Length + data.Length + mac.Length];
        bytes[0] = (byte)type;
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(1, 4), iv.Length);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(5, 4), data.Length);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(9, 4), mac.Length);
        iv.CopyTo(bytes.AsSpan(13));
        data.CopyTo(bytes.AsSpan(13 + iv.Length));
        mac.CopyTo(bytes.AsSpan(13 + iv.Length + data.Length));
        return bytes;
    }
}
