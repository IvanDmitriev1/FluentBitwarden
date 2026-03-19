namespace BitwardenApi;

public sealed record BitwardenEnvironment(
    Uri ApiBase,
    Uri IdentityBase,
    Uri NotificationsBase)
{
    public static BitwardenEnvironment UnitedStates { get; } = new(
        new Uri("https://api.bitwarden.com", UriKind.Absolute),
        new Uri("https://identity.bitwarden.com", UriKind.Absolute),
        new Uri("https://notifications.bitwarden.com", UriKind.Absolute));

    public static BitwardenEnvironment Europe { get; } = new(
        new Uri("https://api.bitwarden.eu", UriKind.Absolute),
        new Uri("https://identity.bitwarden.eu", UriKind.Absolute),
        new Uri("https://notifications.bitwarden.eu", UriKind.Absolute));
}
