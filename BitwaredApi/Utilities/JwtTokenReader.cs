using System.Text;
using System.Text.Json;

namespace BitwaredApi.Utilities;

internal static class JwtTokenReader
{
    public static string? GetClaim(string token, string claimType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentException.ThrowIfNullOrWhiteSpace(claimType);

        string[] parts = token.Split('.');

        if (parts.Length < 2)
        {
            return null;
        }

        string payload = parts[1]
            .Replace('-', '+')
            .Replace('_', '/');

        int padding = 4 - (payload.Length % 4);
        if (padding is > 0 and < 4)
        {
            payload = payload.PadRight(payload.Length + padding, '=');
        }

        byte[] jsonBytes = Convert.FromBase64String(payload);

        try
        {
            using JsonDocument document = JsonDocument.Parse(jsonBytes);
            return document.RootElement.TryGetProperty(claimType, out JsonElement property)
                ? property.GetString()
                : null;
        }
        finally
        {
            Array.Clear(jsonBytes, 0, jsonBytes.Length);
        }
    }
}
