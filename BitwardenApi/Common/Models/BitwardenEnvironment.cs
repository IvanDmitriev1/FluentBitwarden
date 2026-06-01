using MemoryPack;

namespace BitwardenApi.Models;

[MemoryPackable]
public sealed partial record BitwardenEnvironment(
    Uri ApiBase,
    Uri IdentityBase,
    Uri NotificationsBase,
    Uri VaultBase)
{
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
