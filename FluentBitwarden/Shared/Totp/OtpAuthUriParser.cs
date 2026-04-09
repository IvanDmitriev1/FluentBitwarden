using System.Diagnostics.CodeAnalysis;
using OtpNet;

namespace FluentBitwarden.Shared.Totp;

public static class OtpAuthUriParser
{
    private const string Scheme = "otpauth://";

    public static bool TryParse(ReadOnlySpan<char> uri, [NotNullWhen(true)] out OtpAuthData? result)
    {
        result = null;

        if (!uri.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase))
            return false;

        uri = uri[Scheme.Length..];

        int slash = uri.IndexOf('/');
        if (slash < 0) return false;
        if (!Enum.TryParse(uri[..slash].ToString(), ignoreCase: true, out OtpType type)) 
            return false;

        uri = uri[(slash + 1)..];

        int query = uri.IndexOf('?');
        var label = query >= 0 ? uri[..query] : uri;
        uri = query >= 0 ? uri[(query + 1)..] : default;

        int colon = label.IndexOf(':');
        var issuer = colon >= 0 ? label[..colon].ToString() : null;
        var account = colon >= 0 ? label[(colon + 1)..].ToString() : label.ToString();

        string? secret = null;
        var algorithm = OtpHashMode.Sha1;
        int digits = 6;
        int period = 30;
        long counter = 0;

        foreach (var pair in new QueryEnumerator(uri))
        {
            int eq = pair.IndexOf('=');
            if (eq < 0)
                continue;

            var key = pair[..eq];
            var value = pair[(eq + 1)..];

            if (key.Equals("secret", StringComparison.OrdinalIgnoreCase))
                secret = value.ToString();
            else if (key.Equals("issuer", StringComparison.OrdinalIgnoreCase)) 
                issuer = value.ToString();
            else if (key.Equals("algorithm", StringComparison.OrdinalIgnoreCase))
                Enum.TryParse(value.ToString(), ignoreCase: true, out algorithm);
            else if (key.Equals("digits", StringComparison.OrdinalIgnoreCase))
                int.TryParse(value, out digits);
            else if (key.Equals("period", StringComparison.OrdinalIgnoreCase))
                int.TryParse(value, out period);
            else if (key.Equals("counter", StringComparison.OrdinalIgnoreCase))
                long.TryParse(value, out counter);
        }

        if (secret is null) return false;

        result = new OtpAuthData(type, secret, account, issuer, algorithm, digits, period, counter);
        return true;
    }

    private ref struct QueryEnumerator(ReadOnlySpan<char> query)
    {
        private ReadOnlySpan<char> _remaining = query;
        public ReadOnlySpan<char> Current { get; private set; }

        public QueryEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (_remaining.IsEmpty)
                return false;

            int amp = _remaining.IndexOf('&');
            Current = amp >= 0 ? _remaining[..amp] : _remaining;
            _remaining = amp >= 0 ? _remaining[(amp + 1)..] : default;
            return true;
        }
    }
}
