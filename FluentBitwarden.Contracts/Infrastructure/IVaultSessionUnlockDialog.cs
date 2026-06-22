namespace FluentBitwarden.Contracts.Infrastructure;

public interface IVaultSessionUnlockDialog
{
    Task WaitUntilUnlockAsync(CancellationToken cancellationToken);
}
