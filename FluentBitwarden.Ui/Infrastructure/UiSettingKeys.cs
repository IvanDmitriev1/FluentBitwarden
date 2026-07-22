using FluentBitwarden.Platform.Settings.Composition;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Infrastructure;

internal static class UiSettingKeys
{
    public static class Appearance
    {
        public static readonly SettingKey<ElementTheme> ThemeKey =
            new("appearance.theme", ElementTheme.Default);

        public static readonly SettingKey<string> LanguageKey =
            new("appearance.language", "system");
    }

    public static class Vault
    {
        public static readonly CompositeSettingKey<VaultBrowseState> StateKey =
            new("vault.state", VaultBrowseState.Default);
    }
}
