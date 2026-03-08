namespace FluentBitwarden.Models.Session;

public sealed record LoginSessionDisplay(
    string Email,
    string EnvironmentLabel)
{
    public static LoginSessionDisplay Empty { get; } = new(string.Empty, string.Empty);
}
