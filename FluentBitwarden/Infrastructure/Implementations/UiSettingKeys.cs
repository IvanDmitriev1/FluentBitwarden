using FluentBitwarden.Contracts.AppState.Models;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Infrastructure.Implementations;

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
