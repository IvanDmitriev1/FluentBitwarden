using BitwardenApi.Client;
using BitwardenApi.Primitives.Ids;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.Infrastructure.UiActivation;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Ui.Application;
using FluentBitwarden.Ui.Screens.Loading;
using FluentBitwarden.Ui.Screens.Setup;
using FluentBitwarden.Ui.Screens.Shell;
using FluentBitwarden.Ui.Screens.Unlock;
using FluentBitwarden.Ui.Tests.TestInfrastructure;
using FluentBitwarden.Ui.Windows.Abstractions;
using Navis.WinUI.Abstractions;
using NSubstitute;

namespace FluentBitwarden.Ui.Tests.Application;

[Trait("Category", "Unit")]
public sealed class AppRouterTests
{
    private static readonly AccountProfile Account = new(
        UserId.Parse("11111111-1111-1111-1111-111111111111"),
        TestCredentials.Email,
        BitwardenEnvironment.Europe);

    [Fact]
    public void MainWindowRoutesNullStateToLoading()
    {
        var (router, windowManager, navigation) = CreateRouter();

        router.Activate(new UiActivationCommand.OpenCommand(), null);

        windowManager.Received(1).EnsureWindow(WindowKind.Main);
        navigation.Received(1).Navigate<LoadingPage>(NavigationKind.Reset);
    }

    [Fact]
    public void MainWindowRoutesSignedOutStateToSetup()
    {
        var (router, windowManager, navigation) = CreateRouter();

        router.Activate(new UiActivationCommand.OpenCommand(), new AppSessionStateSnapshot.NotAuthenticated());

        windowManager.Received(1).EnsureWindow(WindowKind.Main);
        navigation.Received(1).Navigate<SetupPage>(NavigationKind.Reset);
    }

    [Fact]
    public void MainWindowRoutesLockedStateToUnlockWithTheAccount()
    {
        var (router, windowManager, navigation) = CreateRouter();

        router.Activate(new UiActivationCommand.OpenCommand(), new AppSessionStateSnapshot.Locked(Account));

        windowManager.Received(1).EnsureWindow(WindowKind.Main);
        navigation.Received(1).Navigate<UnlockPage, AccountProfile>(Account, NavigationKind.Reset);
    }

    [Fact]
    public void MainWindowRoutesUnlockedStateToShellWithTheAccount()
    {
        var (router, windowManager, navigation) = CreateRouter();

        router.Activate(new UiActivationCommand.OpenCommand(), new AppSessionStateSnapshot.Unlocked(Account));

        windowManager.Received(1).EnsureWindow(WindowKind.Main);
        navigation.Received(1).Navigate<ShellPage, AccountProfile>(Account, NavigationKind.Reset);
    }

    [Fact]
    public void OpenCommandActivatesMainWindow()
    {
        var (router, windowManager, _) = CreateRouter();

        router.Activate(new UiActivationCommand.OpenCommand(), null);

        windowManager.Received(1).EnsureWindow(WindowKind.Main);
        windowManager.DidNotReceive().EnsureWindow(WindowKind.Overlay);
    }

    [Fact]
    public void OpenItemCommandActivatesMainWindow()
    {
        var (router, windowManager, navigation) = CreateRouter();

        router.Activate(new UiActivationCommand.OpenItemCommand(CipherId.Parse("item-1")), new AppSessionStateSnapshot.Unlocked(Account));

        windowManager.Received(1).EnsureWindow(WindowKind.Main);
        navigation.Received(1).Navigate<ShellPage, AccountProfile>(Account, NavigationKind.Reset);
    }

    [Fact]
    public void OverlayActivationWithNullStateRoutesToLoading()
    {
        var (router, windowManager, navigation) = CreateRouter();

        router.Activate(new UiActivationCommand.OverlayCommand(), null);

        windowManager.Received(1).EnsureWindow(WindowKind.Overlay);
        navigation.Received(1).Navigate<LoadingPage>(NavigationKind.Reset);
        windowManager.DidNotReceive().CloseActiveWindow();
    }

    [Fact]
    public void OverlayActivationWithLockedStateRoutesToUnlockWithTheAccount()
    {
        var (router, windowManager, navigation) = CreateRouter();

        router.Activate(new UiActivationCommand.OverlayCommand(), new AppSessionStateSnapshot.Locked(Account));

        windowManager.Received(1).EnsureWindow(WindowKind.Overlay);
        navigation.Received(1).Navigate<UnlockPage, AccountProfile>(Account, NavigationKind.Reset);
    }

    [Fact]
    public void OverlayActivationWithUnlockedStateLoadsWithoutClosingTheWindow()
    {
        var (router, windowManager, navigation) = CreateRouter();

        router.Activate(new UiActivationCommand.OverlayCommand(), new AppSessionStateSnapshot.Unlocked(Account));

        windowManager.Received(1).EnsureWindow(WindowKind.Overlay);
        navigation.Received(1).Navigate<LoadingPage>(NavigationKind.Reset);
        windowManager.DidNotReceive().CloseActiveWindow();
    }

    [Fact]
    public void OverlayActivationWhenSignedOutUsesMainSetupWindow()
    {
        var (router, windowManager, navigation) = CreateRouter();

        router.Activate(new UiActivationCommand.OverlayCommand(), new AppSessionStateSnapshot.NotAuthenticated());

        windowManager.Received(1).EnsureWindow(WindowKind.Main);
        windowManager.DidNotReceive().EnsureWindow(WindowKind.Overlay);
        navigation.Received(1).Navigate<SetupPage>(NavigationKind.Reset);
    }

    [Fact]
    public void ActivationDoesNotNavigateWhenEnsureWindowReturnsFalse()
    {
        var (router, windowManager, navigation) = CreateRouter(ensureWindow: false);

        router.Activate(new UiActivationCommand.OpenCommand(), null);

        windowManager.Received(1).EnsureWindow(WindowKind.Main);
        Assert.Empty(navigation.ReceivedCalls());
    }

    [Fact]
    public void NavigateDoesNothingWithoutAnActiveWindow()
    {
        var (router, windowManager, navigation) = CreateRouter(activeWindow: null);

        router.Navigate(new AppSessionStateSnapshot.Unlocked(Account));

        Assert.Empty(navigation.ReceivedCalls());
        windowManager.DidNotReceive().EnsureWindow(Arg.Any<WindowKind>());
        windowManager.DidNotReceive().CloseActiveWindow();
    }

    [Fact]
    public void OverlayNavigationWithNullStateRoutesToLoadingWithoutClosing()
    {
        var (router, windowManager, navigation) = CreateRouter(WindowKind.Overlay);

        router.Navigate(null);

        navigation.Received(1).Navigate<LoadingPage>(NavigationKind.Reset);
        windowManager.DidNotReceive().CloseActiveWindow();
    }

    [Fact]
    public void OverlayNavigationWithLockedStateRoutesToUnlockWithTheAccount()
    {
        var (router, windowManager, navigation) = CreateRouter(WindowKind.Overlay);

        router.Navigate(new AppSessionStateSnapshot.Locked(Account));

        navigation.Received(1).Navigate<UnlockPage, AccountProfile>(Account, NavigationKind.Reset);
        windowManager.DidNotReceive().CloseActiveWindow();
    }

    [Fact]
    public void OverlayNavigationWhenSignedOutRedirectsToMainSetupWindow()
    {
        var (router, windowManager, navigation) = CreateRouter(WindowKind.Overlay);

        router.Navigate(new AppSessionStateSnapshot.NotAuthenticated());

        windowManager.Received(1).EnsureWindow(WindowKind.Main);
        navigation.Received(1).Navigate<SetupPage>(NavigationKind.Reset);
    }

    [Fact]
    public void OverlayNavigationWithUnlockedStateLoadsBeforeClosingTheWindow()
    {
        var (router, windowManager, navigation) = CreateRouter(WindowKind.Overlay);

        router.Navigate(new AppSessionStateSnapshot.Unlocked(Account));

        Received.InOrder(() =>
        {
            navigation.Navigate<LoadingPage>(NavigationKind.Reset);
            windowManager.CloseActiveWindow();
        });
    }

    private static (AppRouter Router, IWindowManager WindowManager, INavigation Navigation) CreateRouter(
        WindowKind? activeWindow = null,
        bool ensureWindow = true)
    {
        var windowManager = Substitute.For<IWindowManager>();
        var navigation = Substitute.For<INavigation>();
        windowManager.ActiveWindow.Returns(activeWindow);
        windowManager.WindowNavigation.Returns(navigation);
        windowManager.EnsureWindow(Arg.Any<WindowKind>()).Returns(ensureWindow);
        return (new AppRouter(windowManager), windowManager, navigation);
    }
}
