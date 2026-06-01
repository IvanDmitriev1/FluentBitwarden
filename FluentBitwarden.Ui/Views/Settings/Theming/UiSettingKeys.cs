using FluentBitwarden.Contracts.Modules.AppState.Models;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Views.Settings.Theming;

internal static class UiSettingKeys
{
    public static class Appearance
    {
        public static readonly SettingKey<ElementTheme> ThemeKey =
            new("appearance.theme", ElementTheme.Default);

        public static readonly SettingKey<string> LanguageKey =
            new("appearance.language", "system");
    }
}
