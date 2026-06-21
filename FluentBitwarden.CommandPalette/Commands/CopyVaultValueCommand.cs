using FluentBitwarden.Platform.Clipboard;

namespace FluentBitwarden.CommandPalette.Commands;

internal sealed partial class CopyVaultValueCommand : InvokableCommand
{
    private static readonly TimeSpan VaultCheckTimeout = TimeSpan.FromSeconds(1);

    private readonly AppHostClient _client;
    private readonly string _value;
    private readonly string _valueName;

    public CopyVaultValueCommand(
        AppHostClient client,
        string value,
        string valueName)
    {
        _client = client;
        _value = value;
        _valueName = valueName;
        Name = $"Copy {valueName.ToLowerInvariant()}";
        Icon = Icons.Copy;
    }

    public override ICommandResult Invoke()
    {
        try
        {
            if (!_client.IsVaultUnlocked(VaultCheckTimeout))
                return CommandResult.ShowToast("Unlock FluentBitwarden before copying");

            ClipboardManager.SetText(_value);
            return CommandResult.ShowToast($"{_valueName} copied");
        }
        catch (Exception)
        {
            return CommandResult.ShowToast($"Could not copy {_valueName.ToLowerInvariant()}");
        }
    }
}
