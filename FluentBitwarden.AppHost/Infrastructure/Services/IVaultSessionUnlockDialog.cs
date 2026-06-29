namespace FluentBitwarden.AppHost.Infrastructure.Services;

internal interface IVaultSessionUnlockDialog
{
    Task WaitUntilUnlockAsync(CancellationToken cancellationToken);
}
