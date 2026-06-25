using FluentBitwarden.Contracts.Infrastructure;

namespace FluentBitwarden.CommandPalette.Commands;

internal sealed partial class OpenUnlockCommand(IVaultSessionUnlockDialog sessionUnlockDialog) : InvokableCommand
{
    public override string Name => "Open FluentBitwarden";

    public override IconInfo Icon => Icons.Unlock;

    public override ICommandResult Invoke()
    {
        _ = sessionUnlockDialog.WaitUntilUnlockAsync(CancellationToken.None);
        return CommandResult.ShowToast("Application opened");
    }
}
