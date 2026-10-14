using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Hosting;
using FluentBitwarden.AppHost.Hosting.Activation;
using FluentBitwarden.AppHost.Infrastructure.Processes;
using FluentBitwarden.AppHost.Infrastructure.Tray;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FluentBitwarden.AppHost.IntegrationTests.Hosting;

public sealed class AppHostHostingTests
{
    [Fact]
    public void Start_activation_activates_the_UI()
    {
        var uiProcessLauncher = new FakeUiProcessLauncher();
        var applicationLifetime = Substitute.For<IHostApplicationLifetime>();
        var handler = new AppActivationHandler(AppHostHostingTestSupport.CreateActions(uiProcessLauncher, applicationLifetime));

        handler.Handle(new AppHostCliCommand.Start());

        Assert.Equal(1, uiProcessLauncher.ActivationCount);
        applicationLifetime.DidNotReceive().StopApplication();
    }

    [Fact]
    public void Headless_activation_has_no_action()
    {
        var uiProcessLauncher = new FakeUiProcessLauncher();
        var applicationLifetime = Substitute.For<IHostApplicationLifetime>();
        var handler = new AppActivationHandler(AppHostHostingTestSupport.CreateActions(uiProcessLauncher, applicationLifetime));

        handler.Handle(new AppHostCliCommand.Headless());

        Assert.Equal(0, uiProcessLauncher.ActivationCount);
        applicationLifetime.DidNotReceive().StopApplication();
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
        var actions = AppHostHostingTestSupport.CreateActions(
            new FakeUiProcessLauncher(),
            Substitute.For<IHostApplicationLifetime>(),
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
        AppTray tray = AppHostHostingTestSupport.CreateTray(new FakeUiProcessLauncher(), Substitute.For<IHostApplicationLifetime>());

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
        var applicationLifetime = Substitute.For<IHostApplicationLifetime>();
        AppTray tray = AppHostHostingTestSupport.CreateTray(uiProcessLauncher, applicationLifetime);

        tray.Activate();

        Assert.Equal(1, uiProcessLauncher.ActivationCount);
        applicationLifetime.DidNotReceive().StopApplication();
    }

    [Fact]
    public void Tray_show_menu_item_activates_the_UI()
    {
        var uiProcessLauncher = new FakeUiProcessLauncher();
        var applicationLifetime = Substitute.For<IHostApplicationLifetime>();
        AppTray tray = AppHostHostingTestSupport.CreateTray(uiProcessLauncher, applicationLifetime);

        Assert.IsType<TrayMenuItem.Command>(tray.CreateMenu()[0]).Execute();

        Assert.Equal(1, uiProcessLauncher.ActivationCount);
        applicationLifetime.DidNotReceive().StopApplication();
    }

    [Fact]
    public void Tray_exit_menu_item_requests_host_shutdown()
    {
        var uiProcessLauncher = new FakeUiProcessLauncher();
        var applicationLifetime = Substitute.For<IHostApplicationLifetime>();
        AppTray tray = AppHostHostingTestSupport.CreateTray(uiProcessLauncher, applicationLifetime);

        Assert.IsType<TrayMenuItem.Command>(tray.CreateMenu()[3]).Execute();

        applicationLifetime.Received(1).StopApplication();
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
        var actions = AppHostHostingTestSupport.CreateActions(
            new FakeUiProcessLauncher(),
            Substitute.For<IHostApplicationLifetime>(),
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
        AppHostActions actions = AppHostHostingTestSupport.CreateActions(
            new FakeUiProcessLauncher(),
            Substitute.For<IHostApplicationLifetime>(),
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
        AppHostActions actions = AppHostHostingTestSupport.CreateActions(
            new FakeUiProcessLauncher(),
            Substitute.For<IHostApplicationLifetime>(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            logger);

        actions.Lock();
        RecordedLog log = await logger.CriticalLog.Task.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        Assert.Equal(LogLevel.Critical, log.Level);
        Assert.Same(failure, log.Exception);
        Assert.True(scopeDisposed.Task.IsCompleted);
    }

    [Fact]
    public void Tray_menu_selection_id_zero_means_cancel()
    {
        Assert.Null(TrayMenu.GetSelectedCommand(AppHostHostingTestSupport.SelectionItems(static () => { }), 0));
    }

    [Fact]
    public void Tray_menu_selection_of_separator_returns_null()
    {
        Assert.Null(TrayMenu.GetSelectedCommand(AppHostHostingTestSupport.SelectionItems(static () => { }), 3));
    }

    [Fact]
    public void Tray_menu_selection_outside_items_returns_null()
    {
        Assert.Null(TrayMenu.GetSelectedCommand(AppHostHostingTestSupport.SelectionItems(static () => { }), uint.MaxValue));
    }

    [Fact]
    public void Tray_menu_selection_returns_command_without_executing_its_callback()
    {
        bool executed = false;
        IReadOnlyList<TrayMenuItem> items = AppHostHostingTestSupport.SelectionItems(() => executed = true);
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
        services.AddSingleton(Substitute.For<IHostApplicationLifetime>());
        services.AddLogging();
        services.AddAppHostHosting();

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        Assert.Same(provider.GetRequiredService<AppHostActions>(), provider.GetRequiredService<AppHostActions>());
        Assert.Same(provider.GetRequiredService<AppActivationHandler>(), provider.GetRequiredService<AppActivationHandler>());
        Assert.Same(provider.GetRequiredService<AppTray>(), provider.GetRequiredService<AppTray>());
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IHostedService));
    }

}
