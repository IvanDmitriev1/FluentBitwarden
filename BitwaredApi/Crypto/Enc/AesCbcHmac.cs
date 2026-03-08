using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;

namespace BitwaredApi.Crypto.Enc;

internal static class AesCbcHmac
{
    public static byte[] Decrypt(ParsedEncString encString, ReadOnlySpan<byte> key)
    {
        byte[] data = Convert.FromBase64String(encString.Data);

        try
        {
            return encString.Type switch
            {
                EncStringType.AesCbc256_B64 => DecryptAesCbc(encString, data, key[..32]),
                EncStringType.AesCbc256_HmacSha256_B64 => DecryptAesCbcHmac(encString, data, key),
                _ => throw new CryptographicException($"Unsupported symmetric EncString type: {encString.Type}."),
            };
        }
        finally
        {
            CryptographicOperations.ZeroMemory(data);
        }
    }

    private static byte[] DecryptAesCbc(ParsedEncString encString, byte[] cipherBytes, ReadOnlySpan<byte> encryptionKey)
    {
        ArgumentNullException.ThrowIfNull(encString.Iv);
        byte[] iv = Convert.FromBase64String(encString.Iv);

        try
        {
            return DecryptAesCbcPkcs7(cipherBytes, encryptionKey.ToArray(), iv);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(iv);
        }
    }

    private static byte[] DecryptAesCbcHmac(ParsedEncString encString, byte[] cipherBytes, ReadOnlySpan<byte> key)
    {
        ArgumentNullException.ThrowIfNull(encString.Iv);
        ArgumentNullException.ThrowIfNull(encString.Mac);

        if (key.Length < 64)
        {
            throw new CryptographicException("A 64-byte key is required for HMAC-protected EncStrings.");
        }

        byte[] iv = Convert.FromBase64String(encString.Iv);
        byte[] mac = Convert.FromBase64String(encString.Mac);
        byte[] macInput = new byte[iv.Length + cipherBytes.Length];
        byte[] encryptionKey = key[..32].ToArray();
        byte[] macKey = key[32..64].ToArray();

        try
        {
            Array.Copy(iv, 0, macInput, 0, iv.Length);
            Array.Copy(cipherBytes, 0, macInput, iv.Length, cipherBytes.Length);

            byte[] computedMac = ComputeHmacSha256(macKey, macInput);

            try
            {
                if (!CryptographicOperations.FixedTimeEquals(computedMac, mac))
                {
                    throw new CryptographicException("EncString MAC validation failed.");
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(computedMac);
            }

            return DecryptAesCbcPkcs7(cipherBytes, encryptionKey, iv);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(iv);
            CryptographicOperations.ZeroMemory(mac);
            CryptographicOperations.ZeroMemory(macInput);
            CryptographicOperations.ZeroMemory(encryptionKey);
            CryptographicOperations.ZeroMemory(macKey);
        }
    }

    private static byte[] ComputeHmacSha256(byte[] key, byte[] data)
    {
        HMac hmac = new(new Sha256Digest());
        hmac.Init(new KeyParameter(key));
        hmac.BlockUpdate(data, 0, data.Length);

        byte[] output = new byte[hmac.GetMacSize()];
        hmac.DoFinal(output, 0);
        return output;
    }

    private static byte[] DecryptAesCbcPkcs7(byte[] cipherBytes, byte[] key, byte[] iv)
    {
        IBufferedCipher cipher = new PaddedBufferedBlockCipher(new CbcBlockCipher(new AesEngine()));
        cipher.Init(false, new ParametersWithIV(new KeyParameter(key), iv));

        byte[] output = new byte[cipher.GetOutputSize(cipherBytes.Length)];
        int outputLength = cipher.ProcessBytes(cipherBytes, 0, cipherBytes.Length, output, 0);
        outputLength += cipher.DoFinal(output, outputLength);

        if (outputLength == output.Length)
        {
            return output;
        }

        byte[] trimmed = new byte[outputLength];
        Array.Copy(output, trimmed, outputLength);
        CryptographicOperations.ZeroMemory(output);
        return trimmed;
    }
}
