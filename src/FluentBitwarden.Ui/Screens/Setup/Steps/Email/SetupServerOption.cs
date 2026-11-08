using BitwardenApi.Client;

namespace FluentBitwarden.Ui.Screens.Setup.Steps.Email;

public sealed record SetupServerOption(string Title, BitwardenEnvironment? Environment)
{
    public static SetupServerOption Us { get; } = new("Bitwarden US", BitwardenEnvironment.UnitedStates);
    public static SetupServerOption Eu { get; } = new("Bitwarden EU", BitwardenEnvironment.Europe);
    public static SetupServerOption Custom { get; } = new("Custom", null);

    public bool IsCustom => Environment is null;

    public BitwardenEnvironment ToEnvironment(string customServerUrl)
    {
        if (Environment is not null)
            return Environment;

        string baseUrl = customServerUrl.Trim().TrimEnd('/');
        return new BitwardenEnvironment(
            new Uri($"{baseUrl}/api", UriKind.Absolute),
            new Uri($"{baseUrl}/identity", UriKind.Absolute),
            new Uri($"{baseUrl}/notifications", UriKind.Absolute),
            new Uri(baseUrl, UriKind.Absolute));
    }
}
