namespace FluentBitwarden.Views.LogIn.Models;

public readonly record struct LogInEnvironmentOption(string Title, string Url)
{
    public static readonly LogInEnvironmentOption Us = new("Bitwarden US", "bitwarden.com");
    public static readonly LogInEnvironmentOption Eu = new("Bitwarden EU", "bitwarden.eu");
}