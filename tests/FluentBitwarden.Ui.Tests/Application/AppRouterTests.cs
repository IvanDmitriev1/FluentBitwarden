using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.Infrastructure.UiActivation;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Ui.Application;
using FluentBitwarden.Ui.Screens.Loading;
using FluentBitwarden.Ui.Screens.Setup;
using FluentBitwarden.Ui.Screens.Unlock;
using FluentBitwarden.Ui.Tests.TestInfrastructure;
using FluentBitwarden.Ui.Windows.Abstractions;
using Microsoft.Extensions.Hosting;
using Navis.WinUI.Abstractions;
using NSubstitute;

namespace FluentBitwarden.Ui.Tests.Application;

[Trait("Category", "Unit")]
public sealed class AppRouterTests
{
    private static readonly AccountProfile Account = TestAccounts.CreateEuropean();

    [Fact]
    public void OverlayShowsLoadingUntilSessionArrives()
    {
        var (router, manager, nav, lifetime) = CreateRouter();

        router.Activate(new UiActivationCommand.OverlayCommand(), null);
        manager.CurrentWindowKind.Returns(WindowKind.Overlay);
        router.Navigate(new AppSessionStateSnapshot.Locked(Account));

        manager.Received(1).EnsureWindow(WindowKind.Overlay);
        nav.Received(1).Navigate<LoadingPage>(NavigationKind.Reset);
        nav.Received(1).Navigate<UnlockPage, AccountProfile>(Account, NavigationKind.Reset);
        lifetime.DidNotReceive().StopApplication();
    }

    [Fact]
    public void SignedOutOverlayStopsUiWithoutOpeningWindow()
    {
        var (router, manager, _, lifetime) = CreateRouter();
        using var stopping = new CancellationTokenSource();
        lifetime.ApplicationStopping.Returns(stopping.Token);
        lifetime.When(x => x.StopApplication()).Do(_ => stopping.Cancel());

        var signedOut = new AppSessionStateSnapshot.NotAuthenticated();
        router.Activate(new UiActivationCommand.OverlayCommand(), signedOut);
        manager.DidNotReceive().EnsureWindow(Arg.Any<WindowKind>());
        lifetime.Received(1).StopApplication();
        router.Activate(new UiActivationCommand.OpenCommand(), signedOut);
        router.Navigate(signedOut);
        manager.DidNotReceive().EnsureWindow(Arg.Any<WindowKind>());
        lifetime.Received(1).StopApplication();
    }

    [Fact]
    public void NormalSignedOutLaunchOpensSetup()
    {
        var (router, manager, nav, lifetime) = CreateRouter();

        router.Activate(new UiActivationCommand.OpenCommand(), new AppSessionStateSnapshot.NotAuthenticated());

        manager.Received(1).EnsureWindow(WindowKind.Main);
        nav.Received(1).Navigate<SetupPage>(NavigationKind.Reset);
        lifetime.DidNotReceive().StopApplication();
    }

    [Fact]
    public void SigningOutWithOverlayOpenStopsUiWithoutSwitchingWindows()
    {
        var (router, manager, nav, lifetime) = CreateRouter(WindowKind.Overlay);

        router.Navigate(new AppSessionStateSnapshot.NotAuthenticated());

        manager.DidNotReceive().EnsureWindow(Arg.Any<WindowKind>());
        Assert.Empty(nav.ReceivedCalls());
        lifetime.Received(1).StopApplication();
    }

    [Fact]
    public void OverlayRequestWhileMainExistsReactivatesMainOnSignedOutState()
    {
        var (router, manager, _, lifetime) = CreateRouter(WindowKind.Main);
        router.Activate(new UiActivationCommand.OverlayCommand(), new AppSessionStateSnapshot.NotAuthenticated());
        manager.Received(1).EnsureWindow(WindowKind.Main);
        lifetime.DidNotReceive().StopApplication();
    }

    [Theory]
    [InlineData(WindowKind.Main)]
    [InlineData(WindowKind.Overlay)]
    public void ExistingWindowActivationDoesNotNavigateAcrossKinds(WindowKind existingKind)
    {
        var (router, manager, nav, _) = CreateRouter(existingKind);
        UiActivationCommand command = existingKind == WindowKind.Main
            ? new UiActivationCommand.OverlayCommand()
            : new UiActivationCommand.OpenCommand();

        router.Activate(command, new AppSessionStateSnapshot.Locked(Account));

        manager.Received(1).EnsureWindow(existingKind);
        Assert.Empty(nav.ReceivedCalls());
    }

    private static (AppRouter Router, IWindowManager Manager, INavigation Navigation, IHostApplicationLifetime Lifetime) CreateRouter(
        WindowKind? currentKind = null)
    {
        var manager = Substitute.For<IWindowManager>();
        var navigation = Substitute.For<INavigation>();
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        manager.CurrentWindowKind.Returns(currentKind);
        manager.WindowNavigation.Returns(navigation);
        manager.EnsureWindow(Arg.Any<WindowKind>()).Returns(currentKind is null);
        return (new AppRouter(manager, lifetime), manager, navigation, lifetime);
    }
}
