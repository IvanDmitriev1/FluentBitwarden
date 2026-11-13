using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.AppSession.Unlock;
using FluentBitwarden.Ui.Tests.TestInfrastructure;

namespace FluentBitwarden.Ui.Tests.Windows;

[Collection(DesktopUiCollection.Name)]
[Trait("Category", "Desktop")]
public sealed class MainWindowSessionUiTests(DesktopUiFixture desktop)
{
    [Fact]
    public async Task MainWindowRoutesLoadingLockedUnlockedAndLockedSessions()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await desktop.ResetToSignedOutAsync(cancellationToken);
        await desktop.NavigateAsync(null, cancellationToken);
        Assert.True(await desktop.IsLoadingPageHostedAsync(cancellationToken));

        var account = UiTestComposition.CreateAccount();
        await desktop.PublishSessionStateAsync(new AppSessionStateSnapshot.Locked(account), cancellationToken);
        var automation = await desktop.AutomationAsync(cancellationToken);
        var passwordInput = UiSelectors.ById("PasswordBoxInput", "AccountUnlockPasswordBox");
        var passwordField = await automation.WaitAsync(
            passwordInput,
            "locked account password is enabled and visible",
            static element => element.IsEnabled && !element.IsOffscreen,
            cancellationToken);
        await automation.SetValueAsync(passwordField, TestCredentials.Password, cancellationToken);
        var unlockButton = await automation.WaitAsync(
            UiSelectors.ById("PasswordBoxActionButton", "AccountUnlockPasswordBox"),
            "unlock action is enabled and visible",
            static element => element.IsEnabled && !element.IsOffscreen,
            cancellationToken);
        await automation.InvokeAsync(unlockButton, cancellationToken);

        var unlockRequest = Assert.IsType<SessionUnlockRequest.MasterPasswordRequest>(
            await desktop.WaitForUnlockRequestAsync(cancellationToken));
        Assert.Equal(account.UserId, unlockRequest.UserId);
        Assert.Equal(TestCredentials.Password, unlockRequest.MasterPassword);

        await desktop.PublishSessionStateAsync(new AppSessionStateSnapshot.Unlocked(account), cancellationToken);
        await automation.WaitAsync(
            UiSelectors.ById("ShellNavigationView"),
            "unlocked navigation is visible",
            static element => !element.IsOffscreen,
            cancellationToken);

        await desktop.PublishSessionStateAsync(new AppSessionStateSnapshot.Locked(account), cancellationToken);
        await automation.WaitAsync(
            passwordInput,
            "relocked account password is enabled and visible",
            static element => element.IsEnabled && !element.IsOffscreen,
            cancellationToken);
    }
}
