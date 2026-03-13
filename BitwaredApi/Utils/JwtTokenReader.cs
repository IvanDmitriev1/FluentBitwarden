using System.Text;
using BitwaredApi.Serialization;

namespace BitwaredApi.Utils;

public static class JwtTokenReader
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
            JwtTokenPayloadDto? tokenPayload = System.Text.Json.JsonSerializer.Deserialize(
                jsonBytes,
                BitwaredApiJsonContext.Default.JwtTokenPayloadDto);

            if (tokenPayload is null)
            {
                return null;
            }

            return claimType switch
            {
                "sub" => tokenPayload.Subject,
                "email" => tokenPayload.Email,
                _ => tokenPayload.AdditionalClaims is not null
                    && tokenPayload.AdditionalClaims.TryGetValue(claimType, out System.Text.Json.JsonElement claim)
                    && claim.ValueKind == System.Text.Json.JsonValueKind.String
                        ? claim.GetString()
                        : null,
            };
        }
        finally
        {
            Array.Clear(jsonBytes, 0, jsonBytes.Length);
        }
    }
}
