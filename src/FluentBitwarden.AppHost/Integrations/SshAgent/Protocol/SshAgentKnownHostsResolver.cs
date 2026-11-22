using System.Globalization;
using System.Net;
using System.Security;
using System.Text;

namespace FluentBitwarden.AppHost.Integrations.SshAgent.Protocol;

internal static class SshAgentKnownHostsResolver
{
    private readonly struct HostAddress(ReadOnlyMemory<char> host, ushort? port)
    {
        public ReadOnlyMemory<char> Host { get; } = host;
        public ushort? Port { get; } = port;

        public override string ToString() => Host.ToString();

        public static bool TryParse(ReadOnlyMemory<char> input, out HostAddress address)
        {
            address = default;
            ReadOnlySpan<char> value = input.Span;
            if (value.Length is 0 or > 256)
                return false;

            foreach (char c in value)
            {
                if (!char.IsAsciiLetterOrDigit(c) && c is not ('.' or '-' or ':' or '[' or ']' or '%'))
                    return false;
            }

            ReadOnlyMemory<char> host = input;
            ushort? port = null;
            if (value[0] == '[')
            {
                int hostEnd = value.IndexOf("]:", StringComparison.Ordinal);
                if (hostEnd < 2 || !ushort.TryParse(value[(hostEnd + 2)..], NumberStyles.None, CultureInfo.InvariantCulture, out ushort parsedPort) ||
                    parsedPort == 0)
                {
                    return false;
                }

                host = input[1..hostEnd];
                port = parsedPort;
            }

            ReadOnlySpan<char> hostText = host.Span;
            if (hostText.Contains('[') || hostText.Contains(']') || (!IPAddress.TryParse(hostText, out _) && !IsDnsName(hostText)))
                return false;

            address = new HostAddress(host, port);
            return true;
        }

        private static bool IsDnsName(ReadOnlySpan<char> host) =>
            host.Length <= 253 && host[^1] != '-' && !host.Contains("-.", StringComparison.Ordinal) &&
            Uri.CheckHostName(host.ToString()) == UriHostNameType.Dns;
    }

    private readonly ref struct HostsEntry(
        ReadOnlyMemory<char> hostList,
        ReadOnlySpan<char> keyType,
        ReadOnlySpan<char> key)
    {
        public ReadOnlySpan<HostAddress> Hosts => ParseAddresses(hostList);
        public ReadOnlySpan<char> KeyType { get; } = keyType;
        public ReadOnlySpan<char> Key { get; } = key;

        public static bool TryParse(string rawLine, out HostsEntry entry)
        {
            entry = default;

            const char horizontalTab = '\u0009';
            ReadOnlySpan<char> fieldSeparators = [' ', horizontalTab];
            Span<Range> fields = stackalloc Range[4];

            ReadOnlySpan<char> line = rawLine.AsSpan();
            int fieldCount = line.SplitAny(fields, fieldSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (fieldCount == 0 || line[fields[0]][0] is '#' or '@')
                return false;

            if (fields[..fieldCount] is not [var hosts, var keyType, var key, ..])
                return false;

            entry = new HostsEntry(rawLine.AsMemory()[hosts], line[keyType], line[key]);
            return true;
        }

        private static ReadOnlySpan<HostAddress> ParseAddresses(ReadOnlyMemory<char> hosts)
        {
            var addresses = new HostAddress[hosts.Span.Count(',') + 1];
            int index = 0;
            foreach (Range range in hosts.Span.Split(','))
            {
                if (!HostAddress.TryParse(hosts[range], out addresses[index]))
                    return [];

                index++;
            }

            return addresses;
        }
    }

    public static string? ResolveFromUserFile(ReadOnlyMemory<byte> hostKeyBlob)
    {
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(userProfile))
            return null;

        try
        {
            var filePath = Path.Combine(userProfile, ".ssh", "known_hosts");
            using StreamReader reader = File.OpenText(filePath);
            return Resolve(hostKeyBlob, reader);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException or ArgumentException)
        {
            return null;
        }
    }

    internal static string? Resolve(ReadOnlyMemory<byte> hostKeyBlob, TextReader reader)
    {
        var keyReader = new SshAgentPayloadReader(hostKeyBlob);
        if (!keyReader.TryReadString(out var keyType) || keyType.IsEmpty)
            return null;

        string encodedKey = Convert.ToBase64String(hostKeyBlob.Span);
        HostAddress? resolvedHost = null;

        while (reader.ReadLine() is { } rawLine)
        {
            if (!HostsEntry.TryParse(rawLine, out var entry) ||
                !Ascii.Equals(entry.KeyType, keyType.Span) || !entry.Key.SequenceEqual(encodedKey))
            {
                continue;
            }

            ReadOnlySpan<HostAddress> hosts = entry.Hosts;
            if (hosts.IsEmpty)
                return null;

            foreach (HostAddress host in hosts)
            {
                if (resolvedHost is { } existing &&
                    (host.Port != existing.Port || !host.Host.Span.Equals(existing.Host.Span, StringComparison.OrdinalIgnoreCase)))
                {
                    return null;
                }

                resolvedHost ??= host;
            }
        }

        return resolvedHost?.ToString();
    }
}
