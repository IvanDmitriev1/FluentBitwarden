using System.Diagnostics.CodeAnalysis;

namespace FluentBitwarden.AppHost.Modules.BrowserExtension.Internal;

internal readonly record struct DomainHost(string Value)
{
    public static bool TryCreate(Uri uri, out DomainHost host) =>
        TryCreate(uri.IdnHost, out host);

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase",
        Justification = "Canonicalizing a hostname; lowercase is the correct normalized form (RFC 3986 6.2.2.1), not a security-sensitive case-fold.")]
    public static bool TryCreate(string value, out DomainHost host)
    {
        var canonicalValue = value.Trim().TrimEnd('.').ToLowerInvariant();
        if (canonicalValue.Length == 0)
        {
            host = default;
            return false;
        }

        host = new DomainHost(canonicalValue);
        return true;
    }

    public bool IsSameOrSubdomainOf(DomainHost parent) =>
        string.Equals(Value, parent.Value, StringComparison.OrdinalIgnoreCase) ||
        Value.EndsWith($".{parent.Value}", StringComparison.OrdinalIgnoreCase);
}
