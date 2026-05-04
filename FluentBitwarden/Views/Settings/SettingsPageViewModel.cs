using FluentBitwarden.Modules.AppState.Abstractions;
using FluentBitwarden.Views.Settings.Models;
using Microsoft.UI.Xaml;
using FluentBitwarden.Modules.AppState;

namespace FluentBitwarden.Views.Settings;

public sealed partial class SettingsPageViewModel : ObservableObject
{
    public SettingsPageViewModel(IThemeService themeService)
    {
        Theme = AppSettingKeys.Appearance.ThemeKey.Create(themeService.Apply);
    }

    public SettingValue<ElementTheme> Theme { get; }
}