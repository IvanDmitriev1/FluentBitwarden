using BitwardenApi.Client;
namespace BitwardenApi.Identity.Models;


public sealed record PasswordAuthenticationRequest(
    BitwardenClientContext Context,
    string Email,
    string MasterPasswordHash,
    string Scope = "api offline_access");

