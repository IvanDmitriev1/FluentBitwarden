using FluentBitwarden.Contracts.Modules.Passkey.Models;
using Microsoft.Windows.ApplicationModel.Resources;

namespace FluentBitwarden.Ui.Dialogs.PasskeySignIn;

public sealed class PasskeySignInCandidateViewModel(PasskeySelectionCandidate candidate, string rpId)
{
    private static readonly ResourceLoader Resources = new();

    public CipherId CipherId => candidate.CipherId;
    public string ItemName => string.IsNullOrWhiteSpace(candidate.ItemName)
        ? Resources.GetString("PasskeyFallbackPasskeyName")
        : candidate.ItemName;

    public string AccountName => string.IsNullOrWhiteSpace(candidate.AccountName)
        ? Resources.GetString("PasskeyFallbackAccountUnavailable")
        : candidate.AccountName;

    public string VaultName
    {
        get
        {
            if (candidate.IsPersonalVault)
                return Resources.GetString("PasskeyFallbackPersonalVault");

            return string.IsNullOrWhiteSpace(candidate.OrganizationName)
                ? Resources.GetString("PasskeyFallbackOrganizationVault")
                : candidate.OrganizationName;
        }
    }

    public Uri? SiteIconUri { get; } = GetSiteIconUri(rpId);

    private static Uri? GetSiteIconUri(string host)
    {
        if (Uri.CheckHostName(host) == UriHostNameType.Unknown ||
            !Uri.TryCreate($"https://{host}/", UriKind.Absolute, out Uri? uri) ||
            uri.UserInfo.Length != 0 ||
            !string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return uri;
    }
}
