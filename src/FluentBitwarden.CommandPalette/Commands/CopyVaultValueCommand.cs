using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Contracts.Settings;
using FluentBitwarden.Platform.Infrastructure.Clipboard;

namespace FluentBitwarden.CommandPalette.Commands;

internal sealed partial class CopyVaultValueCommand : InvokableCommand
{
    public CopyVaultValueCommand(string value, string valueName, ISettingsStore settingsStore)
        : this(() => value, valueName, settingsStore)
    {
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase",
        Justification = "Cosmetic UI label casing (e.g. \"Copy password\"), not a security-sensitive identifier.")]
    public CopyVaultValueCommand(Func<string> valueFactory, string valueName, ISettingsStore settingsStore)
    {
        _valueFactory = valueFactory;
        _settingsStore = settingsStore;
        Name = $"Copy {valueName.ToLowerInvariant()}";
        Icon = Icons.Copy;
    }

    private readonly Func<string> _valueFactory;
    private readonly ISettingsStore _settingsStore;

    public override ICommandResult Invoke()
    {
        ClipboardManager.SetText(_valueFactory(), _settingsStore.Get(AppSettingKeys.Clipboard.ClearDelayKey));
        return CommandResult.Hide();
    }
}
