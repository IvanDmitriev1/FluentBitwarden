namespace FluentBitwarden.AppHost.Modules.BrowserExtension.Internal;

internal readonly record struct VaultLoginUri(Uri Value, DomainHost Host)
{
    public bool CanMatchBrowserPage =>
        string.Equals(Value.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Value.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    public static bool TryCreate(string value, out VaultLoginUri loginUri)
    {
        loginUri = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var candidate = value.Trim();
        if (Uri.TryCreate(candidate, UriKind.Absolute, out var absoluteUri))
            return TryCreate(absoluteUri, out loginUri);

        return Uri.TryCreate($"https://{candidate}", UriKind.Absolute, out var webUri) &&
               TryCreate(webUri, out loginUri);
    }

    public bool Matches(BrowserPageUri pageUri) =>
        CanMatchBrowserPage && pageUri.Host.IsSameOrSubdomainOf(Host);

    private static bool TryCreate(Uri uri, out VaultLoginUri loginUri)
    {
        loginUri = default;

        if (!DomainHost.TryCreate(uri, out var host))
            return false;

        loginUri = new VaultLoginUri(uri, host);
        return true;
    }
}
