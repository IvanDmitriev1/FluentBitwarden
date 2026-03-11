using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Services;

internal sealed class LocalVaultStateStore(IAppSettingsStore appSettingsStore) : ILocalVaultStateStore
{
    public async ValueTask<LocalVaultState?> GetForAccountAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        LocalVaultState? state = await appSettingsStore
            .GetLocalVaultStateAsync(accountId, cancellationToken)
            .ConfigureAwait(false);

        if (state?.Payload is not null)
        {
            return state;
        }

        await appSettingsStore.ClearLocalVaultStateAsync(accountId, cancellationToken).ConfigureAwait(false);
        return null;
    }

    public async ValueTask<LocalVaultState> RequireForAccountAsync(
        string accountId,
        CancellationToken cancellationToken = default)
        => await GetForAccountAsync(accountId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("No local vault state is configured for this session.");

    public ValueTask SaveAsync(
        LocalVaultState state,
        CancellationToken cancellationToken = default)
        => appSettingsStore.SaveLocalVaultStateAsync(state, cancellationToken);

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
        => appSettingsStore.ClearAllLocalVaultStatesAsync(cancellationToken);

    public async ValueTask<bool> HasWindowsHelloEnrollmentAsync(
        string accountId,
        CancellationToken cancellationToken = default)
        => (await GetForAccountAsync(accountId, cancellationToken).ConfigureAwait(false))?.WindowsHello is not null;

    public async ValueTask<bool> HasPinEnrollmentAsync(
        string accountId,
        CancellationToken cancellationToken = default)
        => (await GetForAccountAsync(accountId, cancellationToken).ConfigureAwait(false))?.Pin is not null;
}
