using System.Text.RegularExpressions;

namespace FluentBitwarden.AppHost.Modules.BrowserExtension.Internal;

internal static class BrowserLoginUriMatcher
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

    public static bool Matches(LoginVaultCipher cipher, BrowserPageUri pageUri) =>
        cipher.Uris.Any(uri => Matches(uri, pageUri));

    private static bool Matches(LoginUri loginUri, BrowserPageUri pageUri)
    {
        var matchType = loginUri.Match;
        if (matchType == LoginUri.MatchType.Never)
            return false;

        if (matchType == LoginUri.MatchType.RegularExpression)
            return MatchesRegex(loginUri.Value, pageUri.Value.AbsoluteUri);

        if (!loginUri.TryGetWebUri(out var savedUri))
            return false;

        return matchType switch
        {
            LoginUri.MatchType.Domain =>
                DomainHost.TryCreate(savedUri, out var savedHost) &&
                pageUri.Host.IsSameOrSubdomainOf(savedHost),

            LoginUri.MatchType.Host =>
                DomainHost.TryCreate(savedUri, out var savedHost) &&
                string.Equals(savedHost.Value, pageUri.Host.Value, StringComparison.OrdinalIgnoreCase),

            LoginUri.MatchType.StartsWith =>
                GetComparableUri(pageUri.Value).StartsWith(GetComparableUri(savedUri), StringComparison.OrdinalIgnoreCase),

            LoginUri.MatchType.Exact =>
                string.Equals(GetComparableUri(pageUri.Value), GetComparableUri(savedUri), StringComparison.OrdinalIgnoreCase),

            _ => false
        };
    }

    private static bool MatchesRegex(string? pattern, string input)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        try
        {
            return Regex.IsMatch(
                input,
                pattern,
                RegexOptions.CultureInvariant,
                RegexTimeout);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private static string GetComparableUri(Uri uri) => uri.AbsoluteUri.TrimEnd('/');
}
