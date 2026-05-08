using BitwardenApi.Shared.Context;

namespace FluentBitwarden.Views.LogIn.Models;

public readonly record struct LogInEnvironmentOption(string Title, string Url)
{
    public static readonly LogInEnvironmentOption Us = new("Bitwarden US", "bitwarden.com");
    public static readonly LogInEnvironmentOption Eu = new("Bitwarden EU", "bitwarden.eu");
}

public static class LogInEnvironmentOptionExtensions
{
    public static BitwardenEnvironment ToBitwardenEnvironment(this LogInEnvironmentOption selectedEnvironment, string? customServerUrl)
    {
        if (selectedEnvironment == LogInEnvironmentOption.Eu)
        {
            return BitwardenEnvironment.Europe;
        }

        if (selectedEnvironment == LogInEnvironmentOption.Us)
        {
            return BitwardenEnvironment.UnitedStates;
        }

        if (string.IsNullOrWhiteSpace(customServerUrl))
        {
            throw new ArgumentException("Custom server URL is required.", nameof(customServerUrl));
        }

        var baseUrl = customServerUrl.TrimEnd('/');

        return new BitwardenEnvironment(
            new Uri($"{baseUrl}/api", UriKind.Absolute),
            new Uri($"{baseUrl}/identity", UriKind.Absolute),
            new Uri($"{baseUrl}/notifications", UriKind.Absolute),
            new Uri(baseUrl, UriKind.Absolute));
    }
}
