using FluentBitwarden.Platform.Infrastructure.Clipboard;

namespace FluentBitwarden.CommandPalette.Commands;

internal sealed partial class CopyVaultValueCommand : InvokableCommand
{
    public CopyVaultValueCommand(string value, string valueName)
        : this(() => value, valueName)
    {
    }

    public CopyVaultValueCommand(Func<string> valueFactory, string valueName)
    {
        _valueFactory = valueFactory;
        Name = $"Copy {valueName.ToLowerInvariant()}";
        Icon = Icons.Copy;
    }

    private readonly Func<string> _valueFactory;

    public override ICommandResult Invoke()
    {
        ClipboardManager.SetText(_valueFactory());
        return CommandResult.Hide();
    }
}
