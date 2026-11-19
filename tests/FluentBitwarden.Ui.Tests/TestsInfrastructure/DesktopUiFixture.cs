using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.AppSession.Unlock;
using FluentBitwarden.Ui.Application;
using FluentBitwarden.Ui.Application.Abstractions;
using FluentBitwarden.Ui.Infrastructure;
using FluentBitwarden.Ui.Infrastructure.Hosting;
using FluentBitwarden.Ui.Screens.Loading;
using FluentBitwarden.Ui.Screens.Setup;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using FluentBitwarden.Ui.Windows.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.SDK.BuildTools.WinApp.UIAutomation;
using FluentBitwarden.Ui.Features.Vault;
using FluentBitwarden.Platform.Settings.Composition;

namespace FluentBitwarden.Ui.Tests.TestInfrastructure;

public sealed class DesktopUiFixture : IAsyncLifetime
{
    private static readonly TimeSpan LifecycleTimeout = TimeSpan.FromSeconds(30);
    private readonly object _teardownLock = new();
    private readonly UiTestComposition _composition = new();
    private readonly IHost _host;
    private readonly WinUiHostedService<App> _winUi;
    private ServiceProvider? _services;
    private VaultPageViewModel? _vaultPageViewModel;
    private Task? _teardownTask;

    public DesktopUiFixture()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddWinUiHostedService<App>(_ => CreateApp());
        _host = builder.Build();
        _winUi = (WinUiHostedService<App>)_host.Services.GetRequiredService<IHostedService>();
    }

    public async ValueTask InitializeAsync()
    {
        using var timeout = new CancellationTokenSource(LifecycleTimeout);
        try
        {
            await _host.StartAsync(timeout.Token);
            await _winUi.RunOnUiThreadAsync(() =>
                _services!.GetRequiredService<AppSessionMonitor>().StartAsync(timeout.Token)).WaitAsync(timeout.Token);
        }
        catch (Exception startupError)
        {
            try
            {
                await TeardownAsync();
            }
            catch (Exception cleanupError)
            {
                throw new AggregateException("UI startup and cleanup both failed.", startupError, cleanupError);
            }

            throw;
        }
    }

    public ValueTask DisposeAsync() => new(TeardownAsync());

    private App CreateApp()
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        var services = new ServiceCollection();
        _composition.AddTestServices(services);
        services.AddUiServices();
        services.AddTransient<VaultPageViewModel>(sp => _vaultPageViewModel = ActivatorUtilities.CreateInstance<VaultPageViewModel>(sp));
        services.AddSingleton<App, DesktopTestApp>();
        services.AddLogging();
        services.AddWinAppUiAutomation();
        _services = services.BuildServiceProvider();
        var app = _services.GetRequiredService<App>();
        app.DispatcherShutdownMode = Microsoft.UI.Xaml.DispatcherShutdownMode.OnExplicitShutdown;
        return app;
    }

    private Task TeardownAsync()
    {
        lock (_teardownLock)
        {
            return _teardownTask ??= TeardownCoreAsync();
        }
    }

    private async Task TeardownCoreAsync()
    {
        using var timeout = new CancellationTokenSource(LifecycleTimeout);
        List<Exception>? errors = null;

        if (_services is { } services)
        {
            try
            {
                if (_winUi.HasLiveDispatcher)
                {
                    await _winUi.RunOnUiThreadAsync(() => services.GetService<IWindowManager>()?.CloseActiveWindow())
                        .WaitAsync(timeout.Token);
                    await _winUi.RunOnUiThreadAsync(async () => await services.DisposeAsync())
                        .WaitAsync(timeout.Token);
                }
                else
                {
                    await services.DisposeAsync().AsTask().WaitAsync(timeout.Token);
                }
            }
            catch (Exception exception)
            {
                (errors ??= []).Add(exception);
            }
        }

        try
        {
            await _host.StopAsync(timeout.Token);
        }
        catch (Exception exception)
        {
            (errors ??= []).Add(exception);
        }

        try
        {
            if (_host is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync().AsTask().WaitAsync(timeout.Token);
            else
                _host.Dispose();
        }
        catch (Exception exception)
        {
            (errors ??= []).Add(exception);
        }

        if (errors is { Count: 1 })
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors is { Count: > 1 })
            throw new AggregateException("UI teardown failed.", errors);
    }

    public async Task ResetToSignedOutAsync(CancellationToken cancellationToken)
    {
        _composition.Reset();
        var signedOut = new AppSessionStateSnapshot.NotAuthenticated();
        bool needsChildDetach = false;
        bool alreadySignedOut = false;
        await RunOnUiThreadAsync(() =>
        {
            var app = App.Current;
            var windowManager = app.GetRequiredService<IWindowManager>();
            windowManager.EnsureWindow(WindowKind.Main);
            var navigation = windowManager.WindowNavigation;
            needsChildDetach = navigation.Child is not null || navigation.CurrentPageType == typeof(SetupPage);
            alreadySignedOut = app.GetRequiredService<IAppSession>().CurrentSession == signedOut;
        }, cancellationToken);

        if (needsChildDetach)
            await DetachChildAsync(cancellationToken);

        await _composition.PublishSessionStateAsync(signedOut, cancellationToken);
        // The session monitor skips navigation when the session state is unchanged.
        if (alreadySignedOut)
            await NavigateAsync(signedOut, cancellationToken);

        var automation = await AutomationAsync(cancellationToken);
        try
        {
            await automation.WaitAsync(
                UiSelectors.ById("SetupEmailTextBox"),
                "enabled and visible after resetting to sign-in",
                static element => element.IsEnabled && !element.IsOffscreen,
                cancellationToken);
        }
        catch (TimeoutException exception)
        {
            string pages = "";
            await RunOnUiThreadAsync(() =>
            {
                var navigation = App.Current.GetRequiredService<IWindowManager>().WindowNavigation;
                pages = $"Window page: {navigation.CurrentPageType?.Name ?? "none"}. Child page: {navigation.Child?.CurrentPageType?.Name ?? "none"}.";
            }, cancellationToken);
            throw new TimeoutException($"{exception.Message} {pages}", exception);
        }
    }

    internal async Task DetachChildAsync(CancellationToken cancellationToken)
    {
        await NavigateAsync(null, cancellationToken);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(DesktopAutomation.Timeout);
        while (!await _winUi.RunOnUiThreadAsync(() =>
        {
            var navigation = App.Current.GetRequiredService<IWindowManager>().WindowNavigation;
            return navigation.CurrentPageType == typeof(LoadingPage) && navigation.Child is null;
        }).WaitAsync(deadline.Token))
        {
            await Task.Delay(25, deadline.Token);
        }

    }

    public void ConfigureAuthenticatorRejection() => _composition.ConfigureAuthenticatorRejection();
    public void ConfigureUnlockRejection(string reason) => _composition.ConfigureUnlockRejection(reason);
    public void SetVaultBrowseState(VaultBrowseState state) => _composition.SetVaultBrowseState(state);
    public VaultBrowseState VaultBrowseState => _composition.VaultBrowseState;
    internal ICompositeSettingsStore SettingsStore => _composition.SettingsStore;

    internal Task SortVaultAsync(VaultCipherSortDirection direction, CancellationToken cancellationToken) =>
        _winUi.RunOnUiThreadAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var viewModel = _vaultPageViewModel ?? throw new InvalidOperationException("The vault page has not been created.");
            return viewModel.SortChangedCommand.ExecuteAsync(direction);
        }).WaitAsync(DesktopAutomation.Timeout, cancellationToken);

    public IAppSessionClient AppSessionClient => _composition.AppSessionClient;
    public IVaultCipherClient VaultCipherClient => _composition.VaultCipherClient;

    public Task<SessionUnlockRequest> WaitForUnlockRequestAsync(CancellationToken cancellationToken) =>
        _composition.WaitForUnlockRequestAsync(cancellationToken);

    public Task RunOnUiThreadAsync(Action action, CancellationToken cancellationToken) =>
        _winUi.RunOnUiThreadAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            action();
        }).WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);

    public Task NavigateAsync(AppSessionStateSnapshot? state, CancellationToken cancellationToken) =>
        RunOnUiThreadAsync(() => App.Current.GetRequiredService<IAppRouter>().Navigate(state), cancellationToken);

    public Task RefreshSessionAsync(CancellationToken cancellationToken) =>
        _winUi.RunOnUiThreadAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return App.Current.GetRequiredService<IAppSession>().RefreshSessionAsync(cancellationToken);
        });

    public async Task PublishSessionStateAsync(AppSessionStateSnapshot state, CancellationToken cancellationToken)
    {
        if (state is AppSessionStateSnapshot.Unlocked && await _winUi.RunOnUiThreadAsync(() =>
        {
            var windows = App.Current.GetRequiredService<IWindowManager>();
            return windows.ActiveWindow == WindowKind.Main &&
                windows.WindowNavigation.CurrentPageType == typeof(SetupPage);
        }).WaitAsync(cancellationToken))
        {
            await DetachChildAsync(cancellationToken);
        }

        await _composition.PublishSessionStateAsync(state, cancellationToken);
    }

    public Task<bool> IsLoadingPageHostedAsync(CancellationToken cancellationToken) =>
        _winUi.RunOnUiThreadAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return App.Current.GetRequiredService<IWindowManager>().WindowNavigation.CurrentPageType == typeof(LoadingPage);
        }).WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);

    public async Task<DesktopAutomation> AutomationAsync(CancellationToken cancellationToken)
    {
        var (handle, ui) = await _winUi.RunOnUiThreadAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var app = App.Current;
            return (
                (nint)(long)app.GetRequiredService<IWindowManager>().NativeWindowHandle,
                app.GetRequiredService<IUiAutomation>());
        }).WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
        return new DesktopAutomation(ui, UiTarget.FromWindowHandle(handle), _services!.GetRequiredService<IMouseInput>());
    }

    public async Task SendKeysAsync(string keys, CancellationToken cancellationToken)
    {
        var handle = await _winUi.RunOnUiThreadAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return (long)App.Current.GetRequiredService<IWindowManager>().NativeWindowHandle;
        }).WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);

        _services!.GetRequiredService<IKeyboardInput>().Send(
            handle,
            KeyStringParser.Parse(keys),
            KeyTransport.SendInput);
    }
}
