using BitwardenApi.Extensions;
using BitwardenApi.Vault.Internal;
using MemoryPack;
using OtpNet;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BitwardenApi.Models;

public sealed partial class TotpValue(TotpValue.State state)
{
    [MemoryPackable]
    public partial record State(
        OtpType Type,
        OtpHashMode HashMode,
        byte[] SecretBytes,
        int Digits,
        int PeriodSeconds);

    internal readonly State StateObj = state;

    private readonly Totp _totp = new(state.SecretBytes,
        step: state.PeriodSeconds,
        mode: state.HashMode,
        totpSize: state.Digits);

    public int Step { get; } = Math.Max(1, state.PeriodSeconds);

    public DateTimeOffset ExpiresAt
    {
        get
        {
            var now = DateTimeOffset.UtcNow;
            var currentUnixSeconds = now.ToUnixTimeSeconds();
            var nextStepUnixSeconds = (currentUnixSeconds / Step + 1) * Step;

            return DateTimeOffset.FromUnixTimeSeconds(nextStepUnixSeconds);
        }
    }

    public string ComputeTotp() => _totp.ComputeTotp();

    public static bool TryParse(Span<byte> value, [NotNullWhen(true)] out TotpValue? result)
    {
        if (value.IsEmpty)
        {
            result = null;
            return false;
        }

        if (value.StartsWith("otpauth://"u8))
        {
            return TryParseUrl(value, out result);
        }

        int bytesWritten = value.RemoveAsciiWhitespaceInPlace();
        return TryFromBase32(value[..bytesWritten], out result);
    }

    private static bool TryFromBase32(Span<byte> value, [NotNullWhen(true)] out TotpValue? totpValue)
    {
        Span<byte> secretBytes = stackalloc byte[Base32.GetMaxDecodedLength(value.Length)];

        if (!Base32.TryDecode(value, secretBytes, out int written))
        {
            totpValue = null;
            return false;
        }

        totpValue = new TotpValue(new State(OtpType.Totp, OtpHashMode.Sha1, secretBytes[..written].ToArray(), 6, 30));
        return true;
    }

    private static bool TryParseUrl(ReadOnlySpan<byte> uri, [NotNullWhen(true)] out TotpValue? result)
    {
        result = null;

        ReadOnlySpan<byte> scheme = "otpauth://"u8;
        uri = uri[scheme.Length..];
        int slash = uri.IndexOf((byte)'/');
        if (slash < 0) return false;
        if (!Enum.TryParse(System.Text.Encoding.ASCII.GetString(uri[..slash]), ignoreCase: true, out OtpType type))
            return false;

        uri = uri[(slash + 1)..];
        int query = uri.IndexOf((byte)'?');
        var label = query >= 0 ? uri[..query] : uri;
        uri = query >= 0 ? uri[(query + 1)..] : default;
        int colon = label.IndexOf((byte)':');
        string? secret = null;
        var algorithm = OtpHashMode.Sha1;
        int digits = 6;
        int period = 30;

        foreach (var pair in new QueryEnumerator(uri))
        {
            int eq = pair.IndexOf((byte)'=');
            if (eq < 0)
                continue;

            var key = pair[..eq];
            var value = pair[(eq + 1)..];

            if (Ascii.EqualsIgnoreCase(key, "secret"u8))
                secret = Encoding.ASCII.GetString(value);
            else if (Ascii.EqualsIgnoreCase(key, "algorithm"u8))
                Enum.TryParse(Encoding.ASCII.GetString(value), ignoreCase: true, out algorithm);
            else if (Ascii.EqualsIgnoreCase(key, "digits"u8))
                Utf8Parser.TryParse(value, out digits, out _);
            else if (Ascii.EqualsIgnoreCase(key, "period"u8))
                Utf8Parser.TryParse(value, out period, out _);
        }

        if (secret is null)
            return false;

        var secretBytes = Base32Encoding.ToBytes(secret);
        result = new TotpValue(new State(type, algorithm, secretBytes, digits, period));
        return true;
    }

    private ref struct QueryEnumerator(ReadOnlySpan<byte> query)
    {
        private ReadOnlySpan<byte> _remaining = query;
        public ReadOnlySpan<byte> Current { get; private set; }
        public QueryEnumerator GetEnumerator() => this;
        public bool MoveNext()
        {
            if (_remaining.IsEmpty)
                return false;

            int amp = _remaining.IndexOf((byte)'&');
            Current = amp >= 0 ? _remaining[..amp] : _remaining;
            _remaining = amp >= 0 ? _remaining[(amp + 1)..] : default;
            return true;
        }
    }

}
