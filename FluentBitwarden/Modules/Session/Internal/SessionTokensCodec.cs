using BitwardenApi.Modules.Identity.Models;
using CommunityToolkit.HighPerformance.Buffers;
using FluentBitwarden.Modules.Session.Models;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace FluentBitwarden.Modules.Session.Internal;

internal static class SessionTokensCodec
{
    private const int Version = 1;
    private const int HeaderSize = (sizeof(int) * 4) + sizeof(long);
    private const int AccessTokenLengthOffset = sizeof(int);
    private const int RefreshTokenLengthOffset = sizeof(int) * 2;
    private const int TwoFactorTokenLengthOffset = sizeof(int) * 3;
    private const int ExpiresAtOffset = sizeof(int) * 4;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static MemoryOwner<byte> Serialize(SessionTokens tokens, out int bytesWritten)
    {
        var accessTokenLength = StrictUtf8.GetByteCount(tokens.AccessToken.Value);
        var refreshTokenLength = StrictUtf8.GetByteCount(tokens.RefreshToken.Value);
        var twoFactorTokenLength = tokens.TwoFactorToken is { } twoFactorToken
            ? StrictUtf8.GetByteCount(twoFactorToken.Value)
            : -1;

        var payloadLength = checked(
            HeaderSize
            + accessTokenLength
            + refreshTokenLength
            + Math.Max(twoFactorTokenLength, 0));

        var payloadOwner = MemoryOwner<byte>.Allocate(payloadLength);

        try
        {
            var destination = payloadOwner.Span[..payloadLength];
            BinaryPrimitives.WriteInt32LittleEndian(destination, Version);
            BinaryPrimitives.WriteInt32LittleEndian(destination[AccessTokenLengthOffset..], accessTokenLength);
            BinaryPrimitives.WriteInt32LittleEndian(destination[RefreshTokenLengthOffset..], refreshTokenLength);
            BinaryPrimitives.WriteInt32LittleEndian(destination[TwoFactorTokenLengthOffset..], twoFactorTokenLength);
            BinaryPrimitives.WriteInt64LittleEndian(destination[ExpiresAtOffset..], tokens.ExpiresAt.ToUnixTimeMilliseconds());

            var offset = HeaderSize;
            offset += StrictUtf8.GetBytes(tokens.AccessToken.Value, destination[offset..]);
            offset += StrictUtf8.GetBytes(tokens.RefreshToken.Value, destination[offset..]);

            if (tokens.TwoFactorToken is { } presentTwoFactorToken)
            {
                offset += StrictUtf8.GetBytes(presentTwoFactorToken.Value, destination[offset..]);
            }

            bytesWritten = offset;
            return payloadOwner;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(payloadOwner.Span);
            payloadOwner.Dispose();
            throw;
        }
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> payload, out SessionTokens? tokens)
    {
        tokens = null;

        if (payload.Length < HeaderSize)
        {
            return false;
        }

        var version = BinaryPrimitives.ReadInt32LittleEndian(payload);
        var accessTokenLength = BinaryPrimitives.ReadInt32LittleEndian(payload[AccessTokenLengthOffset..]);
        var refreshTokenLength = BinaryPrimitives.ReadInt32LittleEndian(payload[RefreshTokenLengthOffset..]);
        var twoFactorTokenLength = BinaryPrimitives.ReadInt32LittleEndian(payload[TwoFactorTokenLengthOffset..]);
        var expiresAtMilliseconds = BinaryPrimitives.ReadInt64LittleEndian(payload[ExpiresAtOffset..]);

        if (version != Version
            || accessTokenLength < 0
            || refreshTokenLength < 0
            || twoFactorTokenLength < -1)
        {
            return false;
        }

        var expectedLength = (long)HeaderSize
            + accessTokenLength
            + refreshTokenLength
            + Math.Max(twoFactorTokenLength, 0);

        if (expectedLength != payload.Length)
        {
            return false;
        }

        try
        {
            var offset = HeaderSize;

            var accessToken = StrictUtf8.GetString(payload[offset..(offset + accessTokenLength)]);
            offset += accessTokenLength;

            var refreshToken = StrictUtf8.GetString(payload[offset..(offset + refreshTokenLength)]);
            offset += refreshTokenLength;

            TwoFactorToken? twoFactorToken = null;
            if (twoFactorTokenLength >= 0)
            {
                twoFactorToken = new TwoFactorToken(
                    StrictUtf8.GetString(payload[offset..(offset + twoFactorTokenLength)]));
                offset += twoFactorTokenLength;
            }

            if (offset != payload.Length)
            {
                return false;
            }

            tokens = new SessionTokens(
                new RefreshToken(refreshToken),
                twoFactorToken,
                new AccessToken(accessToken),
                DateTimeOffset.FromUnixTimeMilliseconds(expiresAtMilliseconds));

            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
