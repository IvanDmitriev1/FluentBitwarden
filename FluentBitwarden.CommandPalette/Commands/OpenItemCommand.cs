using BitwardenApi.Vault.Items.Contracts;

namespace FluentBitwarden.CommandPalette.Commands;

public sealed partial class OpenItemCommand : InvokableCommand
{
    public OpenItemCommand(VaultCipher vaultCipher)
    {
        _vaultCipher = vaultCipher;
        Name = "Open in application";
        Icon = Icons.Application;
    }

    private readonly VaultCipher _vaultCipher;

    public override string Name => "Open item";
    public override IconInfo Icon => Icons.Application;

    public override ICommandResult Invoke()
    {
        FluentBitwardenProcessLauncher.OpenItem(_vaultCipher.Id);
        return CommandResult.ShowToast("Open application");
    }
}