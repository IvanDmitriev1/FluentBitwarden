using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Infrastructure.Abstractions;
using FluentBitwarden.Infrastructure.Implementations;
using FluentBitwarden.UI.Controls.Lifecycle;
using FluentBitwarden.Views.Settings.Models;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using System.Reflection;
using Windows.ApplicationModel;
using Windows.Storage;
using FluentBitwarden.Contracts.AppState;
using FluentBitwarden.Contracts.AppState.Models;
using FluentBitwarden.Contracts.Accounts;

namespace FluentBitwarden.Views.Settings;

public sealed partial class SettingsPageViewModel(
    IThemeService themeService,
    IWindowsHelloUnlockClient windowsHelloUnlockClient)
    : ObservableObject, IPageLifecycleAware
{
    public SettingValue<ElementTheme> Theme { get; } = UiSettingKeys.Appearance.ThemeKey.CreateSettingValue(themeService.Apply);
    public SettingValue<string> Language { get; } = UiSettingKeys.Appearance.LanguageKey.CreateSettingValue();
    public SettingValue<bool> CloseToTray { get; } = AppSettingKeys.App.CloseToTrayKey.CreateSettingValue();

    public SettingValue<VaultTimeout> VaultTimeout { get; } = AppSettingKeys.Security.VaultTimeoutKey.CreateSettingValue();
    public SettingValue<VaultTimeoutTrigger> VaultTimeoutTrigger { get; } = AppSettingKeys.Security.VaultTimeoutTriggerKey.CreateSettingValue();
    public SettingValue<bool> LockWhenSystemLocks { get; } = AppSettingKeys.Security.LockWhenSystemLocksKey.CreateSettingValue();
    public SettingValue<bool> LockWhenDeviceSleeps { get; } = AppSettingKeys.Security.LockWhenDeviceSleepsKey.CreateSettingValue();
    public SettingValue<bool> LockWhenAppHiddenToTray { get; } = AppSettingKeys.Security.LockWhenAppHiddenToTrayKey.CreateSettingValue();

    public SettingValue<ClipboardClearDelay> ClipboardClearDelay { get; } = AppSettingKeys.Clipboard.ClearDelayKey.CreateSettingValue();
    public SettingValue<bool> ClipboardClearOnLock { get; } = AppSettingKeys.Clipboard.ClearOnLockKey.CreateSettingValue();

    public SettingValue<SensitiveActionPolicy> PasskeyUserVerificationPolicy { get; } = AppSettingKeys.Passkeys.UserVerificationPolicyKey.CreateSettingValue();
    public SettingValue<SensitiveActionPolicy> SshUserVerificationPolicy { get; } = AppSettingKeys.SshAgent.UserVerificationPolicyKey.CreateSettingValue();

    public WindowsHelloSettingValue WindowsHello { get; } = new(windowsHelloUnlockClient);
    public PasskeyPluginSettingValue PasskeyPlugin { get; } = new();

    public string AppVersion { get; } = ResolveAppVersion();

    public async Task OnLoadingAsync(CancellationToken cancellationToken)
    {
        PasskeyPlugin.Load();
        await WindowsHello.LoadAsync();
    }

    public void OnUnloading() { }

    [RelayCommand]
    private void OpenAppDataFolder()
    {
        OpenFolder(ApplicationData.Current.LocalFolder.Path);
    }

    [RelayCommand]
    private void OpenLogsFolder()
    {
        string logsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FluentBitwarden",
            "Logs");

        Directory.CreateDirectory(logsFolder);
        OpenFolder(logsFolder);
    }

    [RelayCommand]
    private void ResetSettings()
    {
        Theme.Reset();
        Language.Reset();
        CloseToTray.Reset();
        VaultTimeout.Reset();
        VaultTimeoutTrigger.Reset();
        LockWhenSystemLocks.Reset();
        LockWhenDeviceSleeps.Reset();
        LockWhenAppHiddenToTray.Reset();
        ClipboardClearDelay.Reset();
        ClipboardClearOnLock.Reset();
        PasskeyPlugin.Enabled.Reset();
        PasskeyUserVerificationPolicy.Reset();
        SshUserVerificationPolicy.Reset();
    }

    private static void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
        });
    }

    private static string ResolveAppVersion()
    {
        if (PackageHelper.IsPackaged)
        {
            PackageVersion version = Package.Current.Id.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Development";
    }
}
