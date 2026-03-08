namespace BitwaredApi.Crypto.Enc;

internal enum EncStringType
{
    AesCbc256_B64 = 0,
    AesCbc256_HmacSha256_B64 = 2,
    Rsa2048_OaepSha256_B64 = 4,
    Rsa2048_OaepSha1_B64 = 3,
    Rsa2048_OaepSha256_HmacSha256_B64 = 6,
    Rsa2048_OaepSha1_HmacSha256_B64 = 5,
}

internal sealed record ParsedEncString(
    EncStringType Type,
    string Data,
    string? Iv = null,
    string? Mac = null);

internal static class EncStringParser
{
    public static bool TryParse(string? value, out ParsedEncString? parsed)
    {
        parsed = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string[] headerSplit = value.Split('.', 2, StringSplitOptions.TrimEntries);
        EncStringType type;
        string body;

        if (headerSplit.Length == 2 && int.TryParse(headerSplit[0], out int typeValue))
        {
            type = (EncStringType)typeValue;
            body = headerSplit[1];
        }
        else
        {
            type = EncStringType.AesCbc256_B64;
            body = value;
        }

        string[] pieces = body.Split('|', StringSplitOptions.None);

        parsed = type switch
        {
            EncStringType.AesCbc256_B64 when pieces.Length == 2
                => new ParsedEncString(type, pieces[1], pieces[0]),
            EncStringType.AesCbc256_HmacSha256_B64 when pieces.Length == 3
                => new ParsedEncString(type, pieces[1], pieces[0], pieces[2]),
            EncStringType.Rsa2048_OaepSha1_B64 or EncStringType.Rsa2048_OaepSha256_B64 when pieces.Length == 1
                => new ParsedEncString(type, pieces[0]),
            EncStringType.Rsa2048_OaepSha1_HmacSha256_B64 or EncStringType.Rsa2048_OaepSha256_HmacSha256_B64 when pieces.Length == 2
                => new ParsedEncString(type, pieces[0], null, pieces[1]),
            _ => null,
        };

        return parsed is not null;
    }

    public static bool IsSerialized(string? value) => TryParse(value, out _);
}
