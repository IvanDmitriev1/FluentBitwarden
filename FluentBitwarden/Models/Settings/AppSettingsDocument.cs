using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Models.Settings;

internal sealed record AppSettingsDocument
{
    public string? DeviceIdentifier { get; init; }

    public AppThemePreference ThemePreference { get; init; } = AppThemePreference.System;

    public Dictionary<string, LocalVaultState> Accounts { get; init; } = new(StringComparer.Ordinal);
}
