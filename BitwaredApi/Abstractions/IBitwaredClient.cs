namespace BitwaredApi.Abstractions;

public interface IBitwaredClient
{
    IAuthService Auth { get; }

    IVaultService Vault { get; }

    ValueTask LockAsync(CancellationToken cancellationToken = default);

    ValueTask LogoutAsync(CancellationToken cancellationToken = default);
}
