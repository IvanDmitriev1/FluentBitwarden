using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Settings;
using FluentBitwarden.Contracts.Settings.Models;
using FluentBitwarden.Platform.Infrastructure.Extensions;
using FluentBitwarden.Ui.Features.Settings.Models;
using FluentBitwarden.Ui.Infrastructure;
using FluentBitwarden.Ui.Windows.Abstractions;
using Navis.WinUI.Abstractions;
using Windows.ApplicationModel;
using Windows.Storage;

namespace FluentBitwarden.Ui.Features.Settings;

public sealed partial class SettingsPageViewModel(
    IAppSessionClient appSessionClient,
    IAccountWindowsHelloIntegrationClient windowsHelloClient,
    IWindowManager windowManager,
    ISettingsStore settingsStore) : ObservableObject, INavigationAware
{
    public SettingValue<ElementTheme> Theme { get; } = UiSettingKeys.Appearance.ThemeKey.CreateSettingValue(settingsStore, windowManager.ApplyTheme);
    public SettingValue<string> Language { get; } = UiSettingKeys.Appearance.LanguageKey.CreateSettingValue(settingsStore);
    public SettingValue<bool> CloseToTray { get; } = AppSettingKeys.App.CloseToTrayKey.CreateSettingValue(settingsStore);
    public SettingValue<VaultTimeout> VaultTimeout { get; } = AppSettingKeys.Security.VaultTimeoutKey.CreateSettingValue(settingsStore);
    public SettingValue<VaultTimeoutTrigger> VaultTimeoutTrigger { get; } = AppSettingKeys.Security.VaultTimeoutTriggerKey.CreateSettingValue(settingsStore);
    public SettingValue<bool> LockWhenSystemLocks { get; } = AppSettingKeys.Security.LockWhenSystemLocksKey.CreateSettingValue(settingsStore);
    public SettingValue<bool> LockWhenDeviceSleeps { get; } = AppSettingKeys.Security.LockWhenDeviceSleepsKey.CreateSettingValue(settingsStore);
    public SettingValue<bool> LockWhenAppHiddenToTray { get; } = AppSettingKeys.Security.LockWhenAppHiddenToTrayKey.CreateSettingValue(settingsStore);
    public SettingValue<ClipboardClearDelay> ClipboardClearDelay { get; } = AppSettingKeys.Clipboard.ClearDelayKey.CreateSettingValue(settingsStore);
    public SettingValue<bool> ClipboardClearOnLock { get; } = AppSettingKeys.Clipboard.ClearOnLockKey.CreateSettingValue(settingsStore);
    public SettingValue<SensitiveActionPolicy> PasskeyUserVerificationPolicy { get; } = AppSettingKeys.Passkeys.UserVerificationPolicyKey.CreateSettingValue(settingsStore);
    public SettingValue<SensitiveActionPolicy> SshUserVerificationPolicy { get; } = AppSettingKeys.SshAgent.UserVerificationPolicyKey.CreateSettingValue(settingsStore);
    public WindowsHelloSettingValue WindowsHello { get; } = new(appSessionClient, windowsHelloClient, windowManager);

    public PasskeyPluginSettingValue PasskeyPlugin { get; } = new(settingsStore);
    public BrowserExtensionSettingValue BrowserExtension { get; } = new(settingsStore);
    public string AppVersion { get; } = ResolveAppVersion();

    public async ValueTask OnNavigatedToAsync(CancellationToken cancellationToken)
    {
        BrowserExtension.Load();
        PasskeyPlugin.Load();
        await WindowsHello.LoadAsync(cancellationToken);
    }

    public ValueTask OnNavigatedFromAsync(CancellationToken cancellationToken) =>
        ValueTask.CompletedTask;

    [RelayCommand]
    private void OpenAppDataFolder() => OpenFolder(ApplicationData.Current.LocalFolder.Path);

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
        BrowserExtension.Enabled.Reset();
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
