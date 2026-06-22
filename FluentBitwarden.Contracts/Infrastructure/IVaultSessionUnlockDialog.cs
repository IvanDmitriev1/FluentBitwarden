namespace FluentBitwarden.Contracts.Infrastructure;

public interface IVaultSessionUnlockDialog
{
    ValueTask WaitUntilUnlockAsync(CancellationToken cancellationToken);
}
