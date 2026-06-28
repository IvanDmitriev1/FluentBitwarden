using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.CommandPalette.Infrastructure.Services;

namespace FluentBitwarden.CommandPalette.Commands;

public sealed partial class OpenItemCommand : InvokableCommand
{
    public OpenItemCommand(VaultCipher vaultCipher, IUiProcessManager processManager)
    {
        _vaultCipher = vaultCipher;
        _processManager = processManager;
        Name = "Open in application";
        Icon = Icons.Application;
    }

    private readonly VaultCipher _vaultCipher;
    private readonly IUiProcessManager _processManager;

    public override string Name => "Open item";
    public override IconInfo Icon => Icons.Application;

    public override ICommandResult Invoke()
    {
        _processManager.OpenItem(_vaultCipher.Id);
        return CommandResult.ShowToast("Open application");
    }
}