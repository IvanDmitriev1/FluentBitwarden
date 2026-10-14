using FluentBitwarden.AppHost.Hosting;
using FluentBitwarden.AppHost.Infrastructure.Processes;
using FluentBitwarden.AppHost.Infrastructure.Tray;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FluentBitwarden.AppHost.IntegrationTests.Hosting;

internal sealed class FakeUiProcessLauncher : IUiProcessLauncher
{
    public int ActivationCount { get; private set; }
    public bool IsRunning => false;
    public event Action ProcessExited { add { } remove { } }

    public void Activate() { }
    public void ActivateMainWindow() => ActivationCount++;
    public void ActivateOverlay() { }
    public void Exit() { }
}

internal sealed class UnusedServiceScopeFactory : IServiceScopeFactory
{
    public IServiceScope CreateScope() => throw new InvalidOperationException("This command should not create a scope.");
}

internal sealed class RecordingLogger : ILogger<AppHostActions>
{
    public TaskCompletionSource<RecordedLog> CriticalLog { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (logLevel == LogLevel.Critical)
            CriticalLog.TrySetResult(new RecordedLog(logLevel, exception));
    }
}

internal sealed record RecordedLog(LogLevel Level, Exception? Exception);

internal static class AppHostHostingTestSupport
{
    internal static AppHostActions CreateActions(
        FakeUiProcessLauncher uiProcessLauncher,
        IHostApplicationLifetime applicationLifetime,
        IServiceScopeFactory? scopeFactory = null,
        ILogger<AppHostActions>? logger = null) =>
        new(
            uiProcessLauncher,
            applicationLifetime,
            scopeFactory ?? new UnusedServiceScopeFactory(),
            logger ?? NullLogger<AppHostActions>.Instance);

    internal static AppTray CreateTray(FakeUiProcessLauncher uiProcessLauncher, IHostApplicationLifetime applicationLifetime) =>
        new(CreateActions(uiProcessLauncher, applicationLifetime));

    internal static IReadOnlyList<TrayMenuItem> SelectionItems(Action execute) =>
    [
        new TrayMenuItem.Command("First", execute),
        new TrayMenuItem.Command("Second", execute),
        new TrayMenuItem.Separator(),
        new TrayMenuItem.Command("Last", execute)
    ];
}
