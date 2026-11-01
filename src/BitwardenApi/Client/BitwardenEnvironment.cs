using MemoryPack;

namespace BitwardenApi.Client;

[MemoryPackable]
public sealed partial record BitwardenEnvironment(
    Uri ApiBase,
    Uri IdentityBase,
    Uri NotificationsBase,
    [property: MemoryPackOrder(3)] Uri VaultBase)
{
    [MemoryPackOrder(0)]
    public Uri ApiBase
    {
        get;
        init => field = NormalizeServiceBase(value);
    } = NormalizeServiceBase(ApiBase);

    [MemoryPackOrder(1)]
    public Uri IdentityBase
    {
        get;
        init => field = NormalizeServiceBase(value);
    } = NormalizeServiceBase(IdentityBase);

    [MemoryPackOrder(2)]
    public Uri NotificationsBase
    {
        get;
        init => field = NormalizeServiceBase(value);
    } = NormalizeServiceBase(NotificationsBase);

    private static Uri NormalizeServiceBase(Uri serviceBase)
    {
        var builder = new UriBuilder(serviceBase);
        if (!builder.Path.EndsWith("/", StringComparison.Ordinal))
            builder.Path += "/";

        return builder.Uri;
    }

    [MemoryPackIgnore]
    public static BitwardenEnvironment UnitedStates { get; } = new(
        new Uri("https://api.bitwarden.com", UriKind.Absolute),
        new Uri("https://identity.bitwarden.com", UriKind.Absolute),
        new Uri("https://notifications.bitwarden.com", UriKind.Absolute),
        new Uri("https://vault.bitwarden.com", UriKind.Absolute));

    [MemoryPackIgnore]
    public static BitwardenEnvironment Europe { get; } = new(
        new Uri("https://api.bitwarden.eu", UriKind.Absolute),
        new Uri("https://identity.bitwarden.eu", UriKind.Absolute),
        new Uri("https://notifications.bitwarden.eu", UriKind.Absolute),
        new Uri("https://vault.bitwarden.eu", UriKind.Absolute));
}
