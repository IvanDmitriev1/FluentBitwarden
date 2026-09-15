using FluentBitwarden.CommandPalette.Pages;

namespace FluentBitwarden.CommandPalette.Application;

internal sealed partial class FluentBitwardenCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public FluentBitwardenCommandsProvider(VaultSearchPage vaultSearchPage)
    {
        DisplayName = "FluentBitwarden";
        Icon = Icons.Application;
        _commands =
        [
            new CommandItem(vaultSearchPage)
            {
                Title = "Search FluentBitwarden",
                Subtitle = "Find a login and copy its password",
            },
        ];
    }

    public override ICommandItem[] TopLevelCommands() => _commands;
}
