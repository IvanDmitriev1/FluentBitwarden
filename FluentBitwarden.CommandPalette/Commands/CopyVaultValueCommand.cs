using FluentBitwarden.Platform.Infrastructure.Clipboard;

namespace FluentBitwarden.CommandPalette.Commands;

internal sealed partial class CopyVaultValueCommand : InvokableCommand
{
    private readonly string _value;
    private readonly string _valueName;

    public CopyVaultValueCommand(string value, string valueName)
    {
        _value = value;
        _valueName = valueName;
        Name = $"Copy {valueName.ToLowerInvariant()}";
        Icon = Icons.Copy;
    }

    public override ICommandResult Invoke()
    {

        ClipboardManager.SetText(_value);
        return CommandResult.ShowToast($"{_valueName} copied");
    }
}
