namespace FluentBitwarden.Views.Startup.Models;

public enum StartupFlowTarget
{
    MainShell,
    RequestHost
}

public sealed record LoadingPageParameter(StartupFlowTarget Target)
{
    public static LoadingPageParameter MainShell { get; } = new(StartupFlowTarget.MainShell);
    public static LoadingPageParameter RequestHost { get; } = new(StartupFlowTarget.RequestHost);
}
