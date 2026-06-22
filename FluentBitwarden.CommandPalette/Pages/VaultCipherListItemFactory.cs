using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.Platform.SiteIcons;

namespace FluentBitwarden.CommandPalette.Pages;

internal sealed class VaultCipherListItemFactory(ISiteIconCache siteIconCache)
{
    public IListItem Create(LoginVaultCipher cipher)
    {
        var item = new ListItem(new CopyVaultValueCommand(cipher.Password!, "Password"))
        {
            Title = cipher.Name,
            Subtitle = GetSubtitle(cipher),
            Icon = GetIcon(cipher),
        };

        if (!string.IsNullOrWhiteSpace(cipher.Username))
        {
            item.MoreCommands =
            [
                new CommandContextItem(new CopyVaultValueCommand(cipher.Username, "Username"))
                {
                    Title = "Copy username",
                },
            ];
        }

        return item;
    }

    public ListItem CreateUnlockItem() => new(new OpenUnlockCommand())
    {
        Title = "Unlock FluentBitwarden",
        Subtitle = "Open the app to unlock your vault, then retry",
        Icon = Icons.Unlock,
    };

    public ListItem CreateSearchErrorItem() => new(new NoOpCommand())
    {
        Title = "Could not search FluentBitwarden",
        Subtitle = "Try the search again",
    };

    public ListItem CreateNoResultsItem() => new(new NoOpCommand())
    {
        Title = "No matching logins",
        Subtitle = "Try a different search",
    };

    private IconInfo GetIcon(LoginVaultCipher cipher) =>
        Uri.TryCreate(cipher.Uris.FirstOrDefault(), UriKind.Absolute, out var siteUri)
        && siteIconCache.TryGetCachedFilePath(siteUri) is { } cachedFilePath
            ? new IconInfo(cachedFilePath.LocalPath)
            : Icons.Application;

    private static string GetSubtitle(LoginVaultCipher cipher)
    {
        if (!string.IsNullOrWhiteSpace(cipher.Username))
            return cipher.Username;

        return cipher.Uris.FirstOrDefault(static uri => !string.IsNullOrWhiteSpace(uri))
            ?? "Login";
    }
}
