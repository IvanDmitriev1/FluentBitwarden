using BitwardenApi.Primitives;

namespace BitwardenApi.Identity;

public sealed record ClientCredentialsLoginRequest(
    BitwardenClientContext Context,
    ClientId ClientId,
    ClientSecret ClientSecret,
    string Scope = "api");
