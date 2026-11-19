using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Contracts.Settings;
using FluentBitwarden.Platform.Infrastructure.Clipboard;

namespace FluentBitwarden.CommandPalette.Commands;

internal sealed partial class CopyVaultValueCommand : InvokableCommand
{
    public CopyVaultValueCommand(string value, string valueName, ISettingsStore settingsStore, IClipboardManager clipboardManager)
        : this(() => value, valueName, settingsStore, clipboardManager)
    {
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase",
        Justification = "Cosmetic UI label casing (e.g. \"Copy password\"), not a security-sensitive identifier.")]
    public CopyVaultValueCommand(
        Func<string> valueFactory,
        string valueName,
        ISettingsStore settingsStore,
        IClipboardManager clipboardManager)
    {
        _valueFactory = valueFactory;
        _settingsStore = settingsStore;
        _clipboardManager = clipboardManager;
        Name = $"Copy {valueName.ToLowerInvariant()}";
        Icon = Icons.Copy;
    }

    private readonly Func<string> _valueFactory;
    private readonly ISettingsStore _settingsStore;
    private readonly IClipboardManager _clipboardManager;

    public override ICommandResult Invoke()
    {
        _clipboardManager.SetText(_valueFactory(), _settingsStore.Get(AppSettingKeys.Clipboard.ClearDelayKey));
        return CommandResult.Hide();
    }
}
