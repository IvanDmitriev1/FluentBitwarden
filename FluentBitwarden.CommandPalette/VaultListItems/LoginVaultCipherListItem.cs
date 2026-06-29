using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.CommandPalette.Infrastructure.ProcessManagers;
using FluentBitwarden.Platform.SiteIcons;

namespace FluentBitwarden.CommandPalette.VaultListItems;

internal sealed partial class LoginVaultCipherListItem : ListItem
{
    public LoginVaultCipherListItem(
        LoginVaultCipher cipher,
        ISiteIconCache siteIconCache,
        IUiProcessManager uiProcessManager)
    {
        Title = cipher.Name;
        Subtitle = string.IsNullOrWhiteSpace(cipher.Username)
            ? "Login"
            : cipher.Username;
        Icon = GetIcon(cipher, siteIconCache);

        if (cipher.Reprompt)
        {
            Command = new OpenItemCommand(cipher, uiProcessManager);
            return;
        }

        var commands = CreateCommands(cipher, uiProcessManager);
        var primaryCommand = commands[0];

        Command = primaryCommand;
        var moreCommands = CreateMoreCommands(commands, primaryCommand);
        if (moreCommands.Length != 0)
            MoreCommands = moreCommands;
    }

    private static List<ICommand> CreateCommands(LoginVaultCipher cipher, IUiProcessManager uiProcessManager)
    {
        var commands = new List<ICommand>(4);

        if (CreateCopyCommand(cipher.Password, "Password") is { } passwordCommand)
            commands.Add(passwordCommand);

        if (CreateCopyCommand(cipher.Username, "Username") is { } usernameCommand)
            commands.Add(usernameCommand);

        if (cipher.Totp is not null)
            commands.Add(new CopyVaultValueCommand(cipher.Totp.ComputeTotp, "TOTP"));

        commands.Add(new OpenItemCommand(cipher, uiProcessManager));

        return commands;
    }

    private static IContextItem[] CreateMoreCommands(IEnumerable<ICommand> commands, ICommand primaryCommand)
    {
        List<IContextItem> contextItems = [];
        foreach (var command in commands)
        {
            if (ReferenceEquals(command, primaryCommand))
                continue;

            contextItems.Add(new CommandContextItem(command));
        }

        return [.. contextItems];
    }

    private static IconInfo GetIcon(LoginVaultCipher cipher, ISiteIconCache siteIconCache) =>
        Uri.TryCreate(cipher.Uris.FirstOrDefault(), UriKind.Absolute, out var siteUri)
        && siteIconCache.TryGetCachedFilePath(siteUri) is { } cachedFilePath
            ? new IconInfo(cachedFilePath.LocalPath)
            : Icons.Login;

    private static ICommand? CreateCopyCommand(string? value, string valueName) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : new CopyVaultValueCommand(value, valueName);
}
