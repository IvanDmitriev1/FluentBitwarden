namespace FluentBitwarden.CommandPalette.Commands;

internal sealed partial class OpenUnlockCommand : InvokableCommand
{
    public override string Name => "Open FluentBitwarden";

    public override IconInfo Icon => Icons.Unlock;

    public override ICommandResult Invoke() => FluentBitwardenProcessLauncher.OpenUnlockOverlay()
        ? CommandResult.ShowToast("FluentBitwarden opened")
        : CommandResult.ShowToast("Could not open FluentBitwarden");
}
