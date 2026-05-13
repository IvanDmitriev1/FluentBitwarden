using FluentBitwarden.Modules.AppState.Models;
using FluentBitwarden.UI.Models;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Views.Settings.Models;

public sealed class ThemeOptionConverter() : OptionItemConverter<ElementTheme, ThemeOption>(ThemeOption.Options);

public sealed class LanguageOptionConverter() : OptionItemConverter<string, LanguageOption>(LanguageOption.Options);

public sealed class VaultTimeoutOptionConverter()
    : OptionItemConverter<VaultTimeout, VaultTimeoutOption>(VaultTimeoutOption.Options);

public sealed class VaultTimeoutTriggerOptionConverter()
    : OptionItemConverter<VaultTimeoutTrigger, VaultTimeoutTriggerOption>(VaultTimeoutTriggerOption.Options);

public sealed class ClipboardClearDelayOptionConverter()
    : OptionItemConverter<ClipboardClearDelay, ClipboardClearDelayOption>(ClipboardClearDelayOption.Options);

public sealed class SensitiveActionPolicyOptionConverter()
    : OptionItemConverter<SensitiveActionPolicy, SensitiveActionPolicyOption>(SensitiveActionPolicyOption.Options);
