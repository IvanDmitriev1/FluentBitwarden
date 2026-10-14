using System.IdentityModel.Tokens.Jwt;

namespace FluentBitwarden.AppHost.Infrastructure.Extensions;

public static class JwtSecurityTokenExtensions
{
    public static string GetRequiredClaim(this JwtSecurityToken token, string claimType)
    {
        return token.Claims
                   .FirstOrDefault(x => x.Type == claimType)
                   ?.Value
               ?? throw new InvalidDataException(
                   $"Access token does not contain the required '{claimType}' claim.");
    }
}
