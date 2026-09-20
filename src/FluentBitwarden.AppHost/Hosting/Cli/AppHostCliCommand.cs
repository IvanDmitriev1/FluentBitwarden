namespace FluentBitwarden.AppHost.Hosting.Cli;

internal abstract record AppHostCliCommand
{
    public sealed record Start : AppHostCliCommand;
    public sealed record Headless : AppHostCliCommand;
    public sealed record Lock : AppHostCliCommand;
}
