using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Core.Abstractions;
using FluentBitwarden.Models.Vault;
using FluentBitwarden.Services.Storage;

namespace FluentBitwarden.Services;

internal sealed class LocalVaultStateStore(IAppPaths paths) : ILocalVaultStateStore
{
    private readonly ProtectedJsonFileStore<LocalVaultState> _store = new(paths.UnlockStateFilePath);

    public async ValueTask<LocalVaultState?> GetForAccountAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        LocalVaultState? state = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (state is null)
        {
            return null;
        }

        if (!string.Equals(state.AccountId, accountId, StringComparison.Ordinal) || state.Payload is null)
        {
            await _store.ClearAsync(cancellationToken).ConfigureAwait(false);
            return null;
        }

        return state;
    }

    public async ValueTask<LocalVaultState> RequireForAccountAsync(
        string accountId,
        CancellationToken cancellationToken = default)
        => await GetForAccountAsync(accountId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("No local vault state is configured for this session.");

    public ValueTask SaveAsync(
        LocalVaultState state,
        CancellationToken cancellationToken = default)
        => _store.SaveAsync(state, cancellationToken);

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
        => _store.ClearAsync(cancellationToken);

    public async ValueTask<bool> HasWindowsHelloEnrollmentAsync(
        string accountId,
        CancellationToken cancellationToken = default)
        => (await GetForAccountAsync(accountId, cancellationToken).ConfigureAwait(false))?.WindowsHello is not null;

    public async ValueTask<bool> HasPinEnrollmentAsync(
        string accountId,
        CancellationToken cancellationToken = default)
        => (await GetForAccountAsync(accountId, cancellationToken).ConfigureAwait(false))?.Pin is not null;
}
