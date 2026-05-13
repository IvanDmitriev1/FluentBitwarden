using FluentBitwarden.Modules.AppState.Models;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Modules.AppState;

public static class AppSettingKeys
{
    public static class App
    {
        public static readonly SettingKey<bool> CloseToTrayKey =
            new("integration.tray.showIcon", true);

        public static readonly SettingKey<bool> SetupCompletedKey =
            new("app.setupCompleted", false);

        public static readonly SettingKey<string> LastOpenedAccountIdKey =
            new("app.lastOpenedAccountId", string.Empty);
    }

    public static class Appearance
    {
        public static readonly SettingKey<ElementTheme> ThemeKey =
            new("appearance.theme", ElementTheme.Default);

        public static readonly SettingKey<string> LanguageKey =
            new("appearance.language", "system");
    }

    public static class Security
    {
        public static readonly SettingKey<VaultTimeout> VaultTimeoutKey =
            new("security.vaultTimeout", VaultTimeout.FiveMinutes);

        public static readonly SettingKey<VaultTimeoutTrigger> VaultTimeoutTriggerKey =
            new("security.vaultTimeoutTrigger", VaultTimeoutTrigger.AppIdle);

        public static readonly SettingKey<bool> LockWhenSystemLocksKey =
            new("security.lockWhenSystemLocks", true);

        public static readonly SettingKey<bool> LockWhenDeviceSleepsKey =
            new("security.lockWhenDeviceSleeps", true);

        public static readonly SettingKey<bool> LockWhenAppHiddenToTrayKey =
            new("security.lockWhenAppHiddenToTray", false);
    }

    public static class Clipboard
    {
        public static readonly SettingKey<ClipboardClearDelay> ClearDelayKey =
            new("clipboard.clearDelay", ClipboardClearDelay.Seconds30);

        public static readonly SettingKey<bool> ClearOnLockKey =
            new("clipboard.clearOnLock", true);
    }

    public static class Passkeys
    {
        public static readonly SettingKey<bool> PluginEnabledKey =
            new("passkeys.plugin.enabled", false);

        public static readonly SettingKey<SensitiveActionPolicy> UserVerificationPolicyKey =
            new("passkeys.userVerificationPolicy", SensitiveActionPolicy.RequireUserAction);
    }

    public static class SshAgent
    {
        public static readonly SettingKey<SensitiveActionPolicy> UserVerificationPolicyKey =
            new("sshAgent.userVerificationPolicy", SensitiveActionPolicy.RequireUserAction);
    }
}
