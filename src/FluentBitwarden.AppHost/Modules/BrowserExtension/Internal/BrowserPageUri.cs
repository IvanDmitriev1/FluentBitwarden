namespace FluentBitwarden.AppHost.Modules.BrowserExtension.Internal;

internal readonly record struct BrowserPageUri(Uri Value, DomainHost Host)
{
    public static bool TryCreate(string value, out BrowserPageUri pageUri)
    {
        pageUri = default;

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !IsWebUri(uri))
            return false;

        if (!DomainHost.TryCreate(uri, out var host))
            return false;

        pageUri = new BrowserPageUri(uri, host);
        return true;
    }

    private static bool IsWebUri(Uri uri) =>
        string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
}
