using BitwardenApi.Shared.Context;
using System.Text.Json.Serialization;

namespace BitwardenApi.Modules.Identity.Models;

public sealed record PasswordLoginRequest(
    BitwardenClientContext Context,
    string Email,
    string MasterPasswordHash,
    string Scope = "api offline_access");

public sealed record PasswordTwoFactorLoginRequest(
    BitwardenClientContext Context,
    string Email,
    string MasterPasswordHash,
    TwoFactorProof TwoFactor,
    string Scope = "api offline_access");

public sealed record RefreshLoginRequest(
    BitwardenClientContext Context,
    RefreshToken RefreshToken,
    string Scope = "api offline_access");

public sealed record DeviceLoginRequest(
    BitwardenClientContext Context,
    string Email,
    string OneTimeAccessCode,
    AuthRequestId? AuthRequestId = null,
    string Scope = "api offline_access");

public sealed record AuthorizationCodeLoginRequest(
    BitwardenClientContext Context,
    string Code,
    string RedirectUri,
    string CodeVerifier,
    string Scope = "api offline_access");

internal sealed class PreloginRequest
{
    [JsonPropertyName("email")]
    public string? Email { get; init; }
}
