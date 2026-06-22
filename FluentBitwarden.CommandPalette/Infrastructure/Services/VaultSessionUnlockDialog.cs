using FluentBitwarden.Contracts.Infrastructure;

namespace FluentBitwarden.CommandPalette.Infrastructure.Services;

internal sealed class VaultSessionUnlockDialog : IVaultSessionUnlockDialog
{
    public ValueTask WaitUntilUnlockAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
