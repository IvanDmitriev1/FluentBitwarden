using FluentBitwarden.Modules.AppState.Models;
using FluentBitwarden.UI.Converters;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Views.Settings.Models;

public readonly record struct ThemeOption(ElementTheme Value, string Title) : IOptionItem<ElementTheme>
{
    public static readonly ThemeOption[] Options =
    [
        new(ElementTheme.Default, "System"),
        new(ElementTheme.Light, "Light"),
        new(ElementTheme.Dark, "Dark"),
    ];

    public override string ToString() => Title;
}

public readonly record struct VaultTimeoutOption(VaultTimeout Value, string Title)
    : IOptionItem<VaultTimeout>
{
    public static readonly VaultTimeoutOption[] Options =
    [
        new(VaultTimeout.Never, "Never"),
        new(VaultTimeout.OneMinute, "1 minute"),
        new(VaultTimeout.FiveMinutes, "5 minutes"),
        new(VaultTimeout.FifteenMinutes, "15 minutes"),
        new(VaultTimeout.ThirtyMinutes, "30 minutes"),
    ];

    public override string ToString() => Title;
}

public readonly record struct VaultTimeoutTriggerOption(VaultTimeoutTrigger Value, string Title)
    : IOptionItem<VaultTimeoutTrigger>
{
    public static readonly VaultTimeoutTriggerOption[] Options =
    [
        new(VaultTimeoutTrigger.AppIdle, "App idle"),
        new(VaultTimeoutTrigger.SystemIdle, "System idle"),
    ];

    public override string ToString() => Title;
}

public readonly record struct ClipboardClearDelayOption(ClipboardClearDelay Value, string Title)
    : IOptionItem<ClipboardClearDelay>
{
    public static readonly ClipboardClearDelayOption[] Options =
    [
        new(ClipboardClearDelay.Never, "Never"),
        new(ClipboardClearDelay.Seconds10, "10 seconds"),
        new(ClipboardClearDelay.Seconds30, "30 seconds"),
        new(ClipboardClearDelay.Seconds60, "60 seconds"),
        new(ClipboardClearDelay.Minutes2, "2 minutes"),
        new(ClipboardClearDelay.Minutes5, "5 minutes"),
    ];

    public override string ToString() => Title;
}

public readonly record struct SensitiveActionPolicyOption(SensitiveActionPolicy Value, string Title)
    : IOptionItem<SensitiveActionPolicy>
{
    public static readonly SensitiveActionPolicyOption[] Options =
    [
        new(SensitiveActionPolicy.AllowWhenUnlocked, "Allow while unlocked"),
        new(SensitiveActionPolicy.RequireUserAction, "Require approval"),
    ];

    public override string ToString() => Title;
}

public readonly record struct LanguageOption(string Value, string Title) : IOptionItem<string>
{
    public static readonly LanguageOption[] Options =
    [
        new("system", "System default"),
    ];

    public override string ToString() => Title;
}
