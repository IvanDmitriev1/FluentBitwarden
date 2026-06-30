using System.Diagnostics.CodeAnalysis;
using MemoryPack;

namespace BitwardenApi.Vault.Items.Contracts;

[MemoryPackable]
public sealed partial class LoginUri
{
    private Uri? _uri;

    public UriMatchType MatchType { get; set; } = UriMatchType.Domain;

    public string Value
    {
        get;
        set
        {
            field = value.Trim();

            if (Uri.TryCreate(value, UriKind.Absolute, out _uri))
            {
                IsWebUri = string.Equals(_uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(_uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                IsWebUri = false;
            }
        }
    } = string.Empty;

    [MemoryPackIgnore]
    public bool IsWebUri { get; private set; }

    public override string ToString() => Value;

    public bool TryGetAbsoluteUri([NotNullWhen(true)] out Uri? uri)
    {
        uri = _uri;
        return _uri is not null;
    }

    public bool TryGetWebUri([NotNullWhen(true)] out Uri? uri)
    {
        if (string.IsNullOrWhiteSpace(Value))
        {
            uri = null;
            return false;
        }

        if (TryGetAbsoluteUri(out uri) && IsWebUri)
            return true;

        const string defaultWebScheme = "https";
        return Uri.TryCreate($"{defaultWebScheme}://{Value}", UriKind.Absolute, out uri);
    }
}
