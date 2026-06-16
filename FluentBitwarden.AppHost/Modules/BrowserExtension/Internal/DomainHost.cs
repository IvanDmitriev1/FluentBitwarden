namespace FluentBitwarden.AppHost.Modules.BrowserExtension.Internal;

internal readonly record struct DomainHost(string Value)
{
    public static bool TryCreate(Uri uri, out DomainHost host) =>
        TryCreate(uri.IdnHost, out host);

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
