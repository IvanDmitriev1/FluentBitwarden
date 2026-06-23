using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Infrastructure.UiCommand;
using FluentBitwarden.Platform.Ipc.Abstractions;
using FluentBitwarden.Services.Window;
using FluentBitwarden.Views.Accounts;
using FluentBitwarden.Views.Shell;
using FluentBitwarden.Views.Startup;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using Windows.Networking.Connectivity;
using WinUIEx;

namespace FluentBitwarden.Application;

internal sealed class AppCoordinator : IAppCoordinator, IDisposable
{
    public AppCoordinator(
        IAccountsClient accountsClient,
        IIpcEventClient eventClient,
        IWindowManager windowManager)
    {
        _accountsClient = accountsClient;
        _windowManager = windowManager;

        _ = eventClient.Subscribe<VaultSessionStatusChangedEvent>(_ =>
            App.Current.DispatcherQueue.TryEnqueue(RefreshSession));
    }

    private readonly IAccountsClient _accountsClient;
    private readonly IWindowManager _windowManager;

    private Frame? _rootFrame;
    private WindowEx? _window;
    private CancellationTokenSource? _flowCancellation;
    private bool _isOverlay;

    public AppSessionState SessionState { get; private set; } = AppSessionState.Unknown;

    internal void HandleActivation(UiActivationCommand command)
    {
        switch (command)
        {
            case UiActivationCommand.Exit:
                App.Current.Exit();
                break;

            case UiActivationCommand.ShowMainWindow:
                ShowMainWindow();
                break;

            case UiActivationCommand.ShowOverlay:
                ShowOverlayWindow();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(command), command, null);
        }
    }

    public void RefreshSession()
    {
        if (SessionState == AppSessionState.LoggedOut)
        {
            StartSignInFlow();
            return;
        }

        StartFlow();
    }

    public void CompleteSignIn(AccountProfile account)
    {
        StartFlow(account.UserId);
    }

    public void RequireSignIn(AccountProfile _)
    {
        EnsureMainWindow();
        StartSignInFlow();
    }

    public void BeginAddAccount()
    {
        EnsureMainWindow();
        StartSignInFlow();
    }

    public void SwitchAccount(UserId accountId)
    {
        StartFlow(accountId);
    }

    public void LogOut(UserId _)
    {
        StartFlow();
    }

    public void Dispose()
    {
        CancelFlow();
    }

    private void ShowMainWindow()
    {
        if (_windowManager.HasWindow)
        {
            _windowManager.ActiveWindow.ShowAndActivate();
            return;
        }

        CreateMainWindow(startFlow: true);
    }

    private void ShowOverlayWindow()
    {
        if (_windowManager.HasWindow)
        {
            _windowManager.ActiveWindow.ShowAndActivate();
            return;
        }

        var window = new OverlayWindow();
        Attach(window, window.NavigationFrame, isOverlay: true);
        _windowManager.SetWindow(window);
        StartFlow();
    }

    private void EnsureMainWindow()
    {
        if (_window is MainWindow)
            return;

        CreateMainWindow(startFlow: false);
    }

    private void CreateMainWindow(bool startFlow)
    {
        _windowManager.CloseWindow();

        var window = new MainWindow();
        Attach(window, window.NavigationFrame, isOverlay: false);
        _windowManager.SetWindow(window);

        if (startFlow)
            StartFlow();
    }

    private void Attach(WindowEx window, Frame rootFrame, bool isOverlay)
    {
        CancelFlow();

        _window = window;
        _rootFrame = rootFrame;
        _isOverlay = isOverlay;

        window.Closed += OnWindowClosed;
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        if (!ReferenceEquals(sender, _window))
            return;

        ((WindowEx)sender).Closed -= OnWindowClosed;

        CancelFlow();

        _window = null;
        _rootFrame = null;
        _isOverlay = false;

        TransitionTo(AppSessionState.Unknown);
    }

    private void StartFlow(UserId? accountId = null)
    {
        if (_rootFrame is null)
        {
            TransitionTo(AppSessionState.Unknown);
            return;
        }

        CancelFlow();
        _flowCancellation = new CancellationTokenSource();
        TransitionTo(AppSessionState.Unknown);
        Replace<LoadingPage>();

        _ = ResolveSessionAsync(
            accountId,
            _flowCancellation.Token);
    }

    private void StartSignInFlow()
    {
        if (_rootFrame is null)
        {
            TransitionTo(AppSessionState.Unknown);
            return;
        }

        CancelFlow();
        TransitionTo(AppSessionState.Unknown);
        ShowLoggedOut();
    }

    private async Task ResolveSessionAsync(
        UserId? accountId,
        CancellationToken cancellationToken)
    {
        try
        {
            var accounts = await _accountsClient.GetAccountsAsync(cancellationToken);
            var unlockedAccount = await _accountsClient.GetUnlockedAccount(cancellationToken);

            if (unlockedAccount is not null &&
                (accountId is null || unlockedAccount.UserId == accountId))
            {
                ShowUnlocked(unlockedAccount);
                return;
            }

            if (accounts.Length == 0)
            {
                ShowLoggedOut();
                return;
            }

            var selectedAccount =
                accounts.FirstOrDefault(account => account.UserId == accountId)
                ?? accounts[0];

            ShowLocked(accounts, selectedAccount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            //
        }
        catch (Exception exception)
        {
            UnhandledExceptionLogger.WriteException(exception);
        }
    }

    private void ShowLoggedOut()
    {
        EnsureMainWindow();
        TransitionTo(AppSessionState.LoggedOut);

        if (NetworkInformation.HasInternetAccess)
        {
            Replace<LogInFlowPage>();
            return;
        }

        Replace<OfflinePage>(
            PageNavigationParameter.From(
                new OfflinePageParameter(OfflinePageReason.FirstSignInRequiresInternet)));
    }

    private void ShowLocked(AccountProfile[] accounts, AccountProfile selectedAccount)
    {
        TransitionTo(AppSessionState.Locked);

        Replace<UnlockPage>(
            PageNavigationParameter.From(
                new UnlockPageParameter(accounts, selectedAccount)));
    }

    private void ShowUnlocked(AccountProfile unlockedAccount)
    {
        TransitionTo(AppSessionState.Unlocked);

        if (!_isOverlay)
        {
            Replace<ShellPage>();
            return;
        }

        _rootFrame!.BackStack.Clear();
        _rootFrame.ForwardStack.Clear();
        _rootFrame.Content = null;
    }

    private void TransitionTo(AppSessionState state)
    {
        if (SessionState == state)
            return;

        Debug.Assert(
            SessionState == AppSessionState.Unknown || state == AppSessionState.Unknown,
            $"Application session transition '{SessionState}' to '{state}' must pass through Unknown.");

        SessionState = state;
    }

    private void Replace<TPage>(IPageNavigationParameter? parameter = null)
        where TPage : Page
    {
        var frame = _rootFrame;

        if (frame is null || frame.Content is TPage && parameter is null)
            return;

        var pageType = typeof(TPage);
        bool navigated = frame.Navigate(pageType, parameter);

        frame.BackStack.Clear();
        frame.ForwardStack.Clear();

        Debug.Assert(navigated, $"Navigation to {pageType.Name} failed.");
    }

    private void CancelFlow()
    {
        _flowCancellation?.Cancel();
        _flowCancellation?.Dispose();
        _flowCancellation = null;
    }
}