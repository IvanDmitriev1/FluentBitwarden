using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Hosting;
using FluentBitwarden.AppHost.Hosting.Activation;
using FluentBitwarden.AppHost.Infrastructure.Processes;
using FluentBitwarden.AppHost.Infrastructure.Tray;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FluentBitwarden.AppHost.IntegrationTests.Hosting;

public sealed class AppHostHostingTests
{
    [Fact]
    public void Start_activation_activates_the_UI()
    {
        var uiProcessLauncher = new FakeUiProcessLauncher();
        var applicationLifetime = new FakeApplicationLifetime();
        var handler = new AppActivationHandler(TestSupport.CreateActions(uiProcessLauncher, applicationLifetime));

        handler.Handle(new AppHostCliCommand.Start());

        Assert.Equal(1, uiProcessLauncher.ActivationCount);
        Assert.Equal(0, applicationLifetime.StopCount);
    }

    [Fact]
    public void Headless_activation_has_no_action()
    {
        var uiProcessLauncher = new FakeUiProcessLauncher();
        var applicationLifetime = new FakeApplicationLifetime();
        var handler = new AppActivationHandler(TestSupport.CreateActions(uiProcessLauncher, applicationLifetime));

        handler.Handle(new AppHostCliCommand.Headless());

        Assert.Equal(0, uiProcessLauncher.ActivationCount);
        Assert.Equal(0, applicationLifetime.StopCount);
    }

    [Fact]
    public async Task Lock_activation_uses_none_token_and_keeps_its_scope_until_completion()
    {
        var lockStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var lockCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scopeDisposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = Substitute.For<IAppSessionService, IAsyncDisposable>();
        session.LockAsync(Arg.Any<CancellationToken>()).Returns(callInfo =>
        {
            lockStarted.TrySetResult(callInfo.Arg<CancellationToken>());
            return lockCompletion.Task;
        });
        ((IAsyncDisposable)session).DisposeAsync().Returns(_ =>
        {
            scopeDisposed.TrySetResult();
            return ValueTask.CompletedTask;
        });

        var services = new ServiceCollection();
        services.AddScoped<IAppSessionService>(_ => session);
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var actions = TestSupport.CreateActions(
            new FakeUiProcessLauncher(),
            new FakeApplicationLifetime(),
            provider.GetRequiredService<IServiceScopeFactory>());
        var handler = new AppActivationHandler(actions);

        handler.Handle(new AppHostCliCommand.Lock());

        Assert.Equal(CancellationToken.None, await lockStarted.Task);
        Assert.False(scopeDisposed.Task.IsCompleted);
        lockCompletion.SetResult();
        await scopeDisposed.Task;
    }

    [Fact]
    public void Tray_menu_has_expected_labels_and_order()
    {
        AppTray tray = TestSupport.CreateTray(new FakeUiProcessLauncher(), new FakeApplicationLifetime());

        Assert.Collection(tray.CreateMenu(),
            item => Assert.Equal("Show", Assert.IsType<TrayMenuItem.Command>(item).Text),
            item => Assert.Equal("Lock", Assert.IsType<TrayMenuItem.Command>(item).Text),
            item => Assert.IsType<TrayMenuItem.Separator>(item),
            item => Assert.Equal("Exit", Assert.IsType<TrayMenuItem.Command>(item).Text));
    }

    [Fact]
    public void Tray_activation_activates_the_UI()
    {
        var uiProcessLauncher = new FakeUiProcessLauncher();
        var applicationLifetime = new FakeApplicationLifetime();
        AppTray tray = TestSupport.CreateTray(uiProcessLauncher, applicationLifetime);

        tray.Activate();

        Assert.Equal(1, uiProcessLauncher.ActivationCount);
        Assert.Equal(0, applicationLifetime.StopCount);
    }

    [Fact]
    public void Tray_show_menu_item_activates_the_UI()
    {
        var uiProcessLauncher = new FakeUiProcessLauncher();
        var applicationLifetime = new FakeApplicationLifetime();
        AppTray tray = TestSupport.CreateTray(uiProcessLauncher, applicationLifetime);

        Assert.IsType<TrayMenuItem.Command>(tray.CreateMenu()[0]).Execute();

        Assert.Equal(1, uiProcessLauncher.ActivationCount);
        Assert.Equal(0, applicationLifetime.StopCount);
    }

    [Fact]
    public void Tray_exit_menu_item_requests_host_shutdown()
    {
        var uiProcessLauncher = new FakeUiProcessLauncher();
        var applicationLifetime = new FakeApplicationLifetime();
        AppTray tray = TestSupport.CreateTray(uiProcessLauncher, applicationLifetime);

        Assert.IsType<TrayMenuItem.Command>(tray.CreateMenu()[3]).Execute();

        Assert.Equal(1, applicationLifetime.StopCount);
        Assert.Equal(0, uiProcessLauncher.ActivationCount);
    }

    [Fact]
    public async Task Tray_lock_menu_item_uses_none_token_and_keeps_its_scope_until_completion()
    {
        var lockStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var lockCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scopeDisposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = Substitute.For<IAppSessionService, IAsyncDisposable>();
        session.LockAsync(Arg.Any<CancellationToken>()).Returns(callInfo =>
        {
            lockStarted.TrySetResult(callInfo.Arg<CancellationToken>());
            return lockCompletion.Task;
        });
        ((IAsyncDisposable)session).DisposeAsync().Returns(_ =>
        {
            scopeDisposed.TrySetResult();
            return ValueTask.CompletedTask;
        });

        var services = new ServiceCollection();
        services.AddScoped<IAppSessionService>(_ => session);
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var actions = TestSupport.CreateActions(
            new FakeUiProcessLauncher(),
            new FakeApplicationLifetime(),
            provider.GetRequiredService<IServiceScopeFactory>());
        var tray = new AppTray(actions);

        Assert.IsType<TrayMenuItem.Command>(tray.CreateMenu()[1]).Execute();

        Assert.Equal(CancellationToken.None, await lockStarted.Task);
        Assert.False(scopeDisposed.Task.IsCompleted);
        lockCompletion.SetResult();
        await scopeDisposed.Task;
    }

    [Fact]
    public async Task Each_lock_request_uses_a_fresh_session_scope()
    {
        var sessions = new List<IAppSessionService>();
        var scopeDisposals = new List<TaskCompletionSource>();
        var services = new ServiceCollection();
        services.AddScoped<IAppSessionService>(_ =>
        {
            var disposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var session = Substitute.For<IAppSessionService, IAsyncDisposable>();
            session.LockAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            ((IAsyncDisposable)session).DisposeAsync().Returns(_ =>
            {
                disposed.TrySetResult();
                return ValueTask.CompletedTask;
            });
            sessions.Add(session);
            scopeDisposals.Add(disposed);
            return session;
        });
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        AppHostActions actions = TestSupport.CreateActions(
            new FakeUiProcessLauncher(),
            new FakeApplicationLifetime(),
            provider.GetRequiredService<IServiceScopeFactory>());

        actions.Lock();
        actions.Lock();
        await Task.WhenAll(scopeDisposals.Select(disposed => disposed.Task));

        Assert.Equal(2, sessions.Count);
        Assert.NotSame(sessions[0], sessions[1]);
    }

    [Fact]
    public async Task Lock_failure_is_logged_after_its_scope_is_disposed()
    {
        var failure = new InvalidOperationException("Synthetic lock failure.");
        var scopeDisposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var logger = new RecordingLogger();
        var session = Substitute.For<IAppSessionService, IAsyncDisposable>();
        session.LockAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(failure));
        ((IAsyncDisposable)session).DisposeAsync().Returns(_ =>
        {
            scopeDisposed.TrySetResult();
            return ValueTask.CompletedTask;
        });
        var services = new ServiceCollection();
        services.AddScoped<IAppSessionService>(_ => session);
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        AppHostActions actions = TestSupport.CreateActions(
            new FakeUiProcessLauncher(),
            new FakeApplicationLifetime(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            logger);

        actions.Lock();
        RecordedLog log = await logger.CriticalLog.Task;

        Assert.Equal(LogLevel.Critical, log.Level);
        Assert.Same(failure, log.Exception);
        Assert.True(scopeDisposed.Task.IsCompleted);
    }

    [Fact]
    public void Tray_menu_selection_id_zero_means_cancel()
    {
        Assert.Null(TrayMenu.GetSelectedCommand(TestSupport.SelectionItems(static () => { }), 0));
    }

    [Fact]
    public void Tray_menu_selection_of_separator_returns_null()
    {
        Assert.Null(TrayMenu.GetSelectedCommand(TestSupport.SelectionItems(static () => { }), 3));
    }

    [Fact]
    public void Tray_menu_selection_outside_items_returns_null()
    {
        Assert.Null(TrayMenu.GetSelectedCommand(TestSupport.SelectionItems(static () => { }), uint.MaxValue));
    }

    [Fact]
    public void Tray_menu_selection_returns_command_without_executing_its_callback()
    {
        bool executed = false;
        IReadOnlyList<TrayMenuItem> items = TestSupport.SelectionItems(() => executed = true);
        TrayMenuItem.Command expected = Assert.IsType<TrayMenuItem.Command>(items[3]);

        TrayMenuItem.Command? selected = TrayMenu.GetSelectedCommand(items, 4);

        Assert.Same(expected, selected);
        Assert.False(executed);
    }

    [Fact]
    public void Hosting_services_resolve_app_tray_as_a_singleton_with_one_hosted_lifecycle_registration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUiProcessLauncher>(new FakeUiProcessLauncher());
        services.AddSingleton<IHostApplicationLifetime>(new FakeApplicationLifetime());
        services.AddLogging();
        services.AddAppHostHosting();

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        Assert.Same(provider.GetRequiredService<AppHostActions>(), provider.GetRequiredService<AppHostActions>());
        Assert.Same(provider.GetRequiredService<AppActivationHandler>(), provider.GetRequiredService<AppActivationHandler>());
        Assert.Same(provider.GetRequiredService<AppTray>(), provider.GetRequiredService<AppTray>());
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IHostedService));
    }

    private sealed class FakeUiProcessLauncher : IUiProcessLauncher
    {
        public int ActivationCount { get; private set; }
        public bool IsRunning => false;
        public event Action ProcessExited { add { } remove { } }

        public void Activate() { }
        public void ActivateMainWindow() => ActivationCount++;
        public void ActivateOverlay() { }
        public void Exit() { }
    }

    private sealed class FakeApplicationLifetime : IHostApplicationLifetime
    {
        public int StopCount { get; private set; }
        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => CancellationToken.None;
        public CancellationToken ApplicationStopped => CancellationToken.None;
        public void StopApplication() => StopCount++;
    }

    private sealed class UnusedServiceScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => throw new InvalidOperationException("This command should not create a scope.");
    }

    private sealed class RecordingLogger : ILogger<AppHostActions>
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

    private sealed record RecordedLog(LogLevel Level, Exception? Exception);

    private static class TestSupport
    {
        public static AppHostActions CreateActions(
            FakeUiProcessLauncher uiProcessLauncher,
            FakeApplicationLifetime applicationLifetime,
            IServiceScopeFactory? scopeFactory = null,
            ILogger<AppHostActions>? logger = null) =>
            new(
                uiProcessLauncher,
                applicationLifetime,
                scopeFactory ?? new UnusedServiceScopeFactory(),
                logger ?? NullLogger<AppHostActions>.Instance);

        public static AppTray CreateTray(FakeUiProcessLauncher uiProcessLauncher, FakeApplicationLifetime applicationLifetime) =>
            new(CreateActions(uiProcessLauncher, applicationLifetime));

        public static IReadOnlyList<TrayMenuItem> SelectionItems(Action execute) =>
        [
            new TrayMenuItem.Command("First", execute),
            new TrayMenuItem.Command("Second", execute),
            new TrayMenuItem.Separator(),
            new TrayMenuItem.Command("Last", execute)
        ];
    }
}
