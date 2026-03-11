using FluentBitwarden.Models.Settings;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Abstractions;

/// <summary>
/// Persists global app settings and per-account local unlock state.
/// </summary>
internal interface IAppSettingsStore
{
    ValueTask<string> GetOrCreateDeviceIdentifierAsync(CancellationToken cancellationToken = default);

    ValueTask<AppThemePreference> GetThemePreferenceAsync(CancellationToken cancellationToken = default);

    ValueTask SetThemePreferenceAsync(
        AppThemePreference themePreference,
        CancellationToken cancellationToken = default);

    ValueTask<LocalVaultState?> GetLocalVaultStateAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    ValueTask SaveLocalVaultStateAsync(
        LocalVaultState state,
        CancellationToken cancellationToken = default);

    ValueTask ClearLocalVaultStateAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    ValueTask ClearAllLocalVaultStatesAsync(CancellationToken cancellationToken = default);
}
