namespace FluentBitwarden.CommandPalette;

internal sealed partial class FluentBitwardenCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public FluentBitwardenCommandsProvider(AppHostClient client)
    {
        DisplayName = "FluentBitwarden";
        Icon = Icons.Application;
        _commands =
        [
            new CommandItem(new Pages.VaultSearchPage(client))
            {
                Title = "Search FluentBitwarden",
                Subtitle = "Find a login and copy its password",
            },
        ];
    }

    public override ICommandItem[] TopLevelCommands() => _commands;
}
