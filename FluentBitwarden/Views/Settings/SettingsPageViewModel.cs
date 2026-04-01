using FluentBitwarden.Modules.AppState.Abstractions;
using FluentBitwarden.Modules.AppState.Models;
using FluentBitwarden.Shared.Behaviors.Lifecycle;
using FluentBitwarden.Views.Settings.Models;
using Microsoft.UI.Xaml;
using System.ComponentModel;

namespace FluentBitwarden.Views.Settings;

public sealed partial class SettingsPageViewModel(ISettingsService settingsService, IThemeService themeService) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty]
    public partial ThemeOption SelectedThemeOption { get; set; }

    [ObservableProperty]
    public partial bool LockOnSystemSuspend { get; set; }

    [ObservableProperty]
    public partial bool LockOnMinimize { get; set; }

    [ObservableProperty]
    public partial int LockTimeoutMinutes { get; set; }

    [ObservableProperty]
    public partial int ClearClipboardAfterSeconds { get; set; }

    public Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        var settings = settingsService.Get();

        SelectedThemeOption = ThemeOption.Create(settings.ThemeMode);
        LockOnSystemSuspend = settings.LockOnSystemSuspend;
        LockOnMinimize = settings.LockOnMinimize;
        LockTimeoutMinutes = settings.LockTimeoutMinutes;
        ClearClipboardAfterSeconds = settings.ClearClipboardAfterSeconds;

        return Task.CompletedTask;
    }

    public void OnUnloading() { }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        settingsService.Save(BuildSnapshot());
    }

    partial void OnSelectedThemeOptionChanged(ThemeOption value)
    {
        themeService.Set(value.Value);
    }

    private AppSettings BuildSnapshot() => new(
        ThemeMode: SelectedThemeOption.Value,
        LockOnMinimize: LockOnMinimize,
        LockOnSystemSuspend: LockOnSystemSuspend,
        LockTimeoutMinutes: LockTimeoutMinutes,
        ClearClipboardAfterSeconds: ClearClipboardAfterSeconds);
}