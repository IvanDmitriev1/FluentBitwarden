using FluentBitwarden.Abstractions;

namespace FluentBitwarden.Services;

public sealed class BitwaredClient(IAuthService auth, IVaultService vault) : IBitwaredClient
{
    public IAuthService Auth { get; } = auth;
    public IVaultService Vault { get; } = vault;

    public ValueTask LockAsync(CancellationToken cancellationToken = default)
        => Auth.LockAsync(cancellationToken);

    public ValueTask LogoutAsync(CancellationToken cancellationToken = default)
        => Auth.LogoutAsync(cancellationToken);
}
