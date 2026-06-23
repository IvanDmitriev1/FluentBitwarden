using FluentBitwarden.Contracts.Settings.Models;
using FluentBitwarden.Infrastructure.Converters;
using FluentBitwarden.ViewModels.Settings.Models;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Converters;

public sealed partial class ThemeOptionConverter() : OptionItemConverter<ElementTheme, ThemeOption>(ThemeOption.Options);

public sealed partial class LanguageOptionConverter() : OptionItemConverter<string, LanguageOption>(LanguageOption.Options);

public sealed partial class VaultTimeoutOptionConverter()
    : OptionItemConverter<VaultTimeout, VaultTimeoutOption>(VaultTimeoutOption.Options);

public sealed partial class VaultTimeoutTriggerOptionConverter()
    : OptionItemConverter<VaultTimeoutTrigger, VaultTimeoutTriggerOption>(VaultTimeoutTriggerOption.Options);

public sealed partial class ClipboardClearDelayOptionConverter()
    : OptionItemConverter<ClipboardClearDelay, ClipboardClearDelayOption>(ClipboardClearDelayOption.Options);

public sealed partial class SensitiveActionPolicyOptionConverter()
    : OptionItemConverter<SensitiveActionPolicy, SensitiveActionPolicyOption>(SensitiveActionPolicyOption.Options);
