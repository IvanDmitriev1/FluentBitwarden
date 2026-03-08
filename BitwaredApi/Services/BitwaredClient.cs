using BitwaredApi.Abstractions;

namespace BitwaredApi.Services;

public sealed class BitwaredClient : IBitwaredClient
{
    public BitwaredClient(IAuthService auth, IVaultService vault)
    {
        Auth = auth;
        Vault = vault;
    }

    public IAuthService Auth { get; }

    public IVaultService Vault { get; }

    public ValueTask LockAsync(CancellationToken cancellationToken = default)
        => Auth.LockAsync(cancellationToken);

    public ValueTask LogoutAsync(CancellationToken cancellationToken = default)
        => Auth.LogoutAsync(cancellationToken);
}
