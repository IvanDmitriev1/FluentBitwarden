using BitwardenApi.Vault.Ciphers.Models;
using FluentBitwarden.Contracts.AppSession.State;
using FluentBitwarden.Contracts.AppSession.Unlock;
using FluentBitwarden.Contracts.Modules.Vault.Ciphers.Search;
using FluentBitwarden.Ui.Tests.TestInfrastructure;
using NSubstitute;

namespace FluentBitwarden.Ui.Tests.Windows;

[Collection(DesktopUiCollection.Name)]
[Trait("Category", "Desktop")]
public sealed class MainWindowSessionUiTests(DesktopUiFixture desktop)
{
    [Fact]
    public async Task SearchSuggestionsAndQueryAreClearedWhenTheVaultLocksAndUnlocksAgain()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await desktop.ResetToSignedOutAsync(cancellationToken);

        var account = UiTestComposition.CreateAccount();
        Cipher cipher = Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Name = "Synthetic UI search result";
        desktop.VaultCipherClient.SearchCiphersAsync(Arg.Any<VaultCipherQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Cipher[]>([cipher]));
        await desktop.PublishSessionStateAsync(new AppSessionStateSnapshot.Unlocked(account), cancellationToken);

        var automation = await desktop.AutomationAsync(cancellationToken);
        var searchSelector = UiSelectors.ById("TextBox", "VaultCipherSearchBox");
        var searchBox = await automation.WaitAsync(
            searchSelector,
            "search box is enabled and visible",
            static element => element.IsEnabled && !element.IsOffscreen,
            cancellationToken);
        await automation.SetValueAsync(searchBox, "synthetic", cancellationToken);
        var suggestion = UiSelectors.ById("VaultCipherSuggestionName", "VaultCipherSearchBox");
        await automation.WaitAsync(suggestion, "matching suggestion is visible", element => !element.IsOffscreen && element.Name == cipher.Name, cancellationToken);

        await desktop.PublishSessionStateAsync(new AppSessionStateSnapshot.Locked(account), cancellationToken);
        var passwordSelector = UiSelectors.ById("PasswordBoxInput", "AccountUnlockPasswordBox");
        await automation.WaitAsync(
            passwordSelector,
            "locked account password is visible",
            static element => element.IsEnabled && !element.IsOffscreen,
            cancellationToken);

        await desktop.PublishSessionStateAsync(new AppSessionStateSnapshot.Unlocked(account), cancellationToken);
        await automation.WaitAsync(
            UiSelectors.ById("ShellNavigationView"),
            "unlocked navigation is visible again",
            static element => !element.IsOffscreen,
            cancellationToken);
        await automation.WaitAsync(
            searchSelector,
            "search text is empty after re-unlock",
            static element => element.IsEnabled && !element.IsOffscreen && string.IsNullOrEmpty(element.Value),
            cancellationToken);
        await automation.WaitAbsentAsync(suggestion, cancellationToken);
    }

    [Fact]
    public async Task LockEventWinsOverAnOlderSessionRefresh()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await desktop.ResetToSignedOutAsync(cancellationToken);
        var account = UiTestComposition.CreateAccount();
        await desktop.PublishSessionStateAsync(new AppSessionStateSnapshot.Unlocked(account), cancellationToken);
        var automation = await desktop.AutomationAsync(cancellationToken);
        await automation.WaitAsync(
            UiSelectors.ById("ShellNavigationView"),
            "unlocked navigation is visible",
            static element => !element.IsOffscreen,
            cancellationToken);

        var delayedResponse = new TaskCompletionSource<AppSessionStateSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var refreshStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        desktop.AppSessionClient.GetStateAsync(Arg.Any<GetAppSessionStateRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                refreshStarted.TrySetResult(true);
                return delayedResponse.Task;
            });

        Task refresh = desktop.RefreshSessionAsync(cancellationToken);
        try
        {
            await refreshStarted.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            await desktop.PublishSessionStateAsync(new AppSessionStateSnapshot.Locked(account), cancellationToken);
            var passwordSelector = UiSelectors.ById("PasswordBoxInput", "AccountUnlockPasswordBox");
            await automation.WaitAsync(
                passwordSelector,
                "lock event routes to password unlock",
                static element => element.IsEnabled && !element.IsOffscreen,
                cancellationToken);

            delayedResponse.SetResult(new AppSessionStateSnapshot.Unlocked(account));
            await refresh;
            await automation.WaitAsync(
                passwordSelector,
                "stale unlocked snapshot does not undo the lock",
                static element => element.IsEnabled && !element.IsOffscreen,
                cancellationToken);
        }
        finally
        {
            delayedResponse.TrySetResult(new AppSessionStateSnapshot.Unlocked(account));
            await refresh;
        }
    }

    [Fact]
    public async Task RejectedUnlockShowsFeedbackAndSendsTypedRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await desktop.ResetToSignedOutAsync(cancellationToken);
        desktop.ConfigureUnlockRejection("Synthetic unlock rejection.");
        var account = UiTestComposition.CreateAccount();
        await desktop.PublishSessionStateAsync(new AppSessionStateSnapshot.Locked(account), cancellationToken);

        var automation = await desktop.AutomationAsync(cancellationToken);
        var passwordSelector = UiSelectors.ById("PasswordBoxInput", "AccountUnlockPasswordBox");
        var passwordField = await automation.WaitAsync(
            passwordSelector,
            "password field is enabled and visible",
            static element => element.IsEnabled && !element.IsOffscreen,
            cancellationToken);
        await automation.FocusAsync(passwordField, cancellationToken);
        await automation.SetValueAsync(passwordField, TestCredentials.Password, cancellationToken);
        var unlockButton = await automation.WaitAsync(
            UiSelectors.ById("PasswordBoxActionButton", "AccountUnlockPasswordBox"),
            "unlock action is enabled and visible",
            static element => element.IsEnabled && !element.IsOffscreen,
            cancellationToken);
        await automation.InvokeAsync(unlockButton, cancellationToken);

        var request = Assert.IsType<SessionUnlockRequest.MasterPasswordRequest>(
            await desktop.WaitForUnlockRequestAsync(cancellationToken));
        Assert.Equal(account.UserId, request.UserId);
        Assert.Equal(TestCredentials.Password, request.MasterPassword);
        await automation.WaitAsync(
            UiSelectors.ById("UnlockError"),
            "rejection feedback is visible",
            element => !element.IsOffscreen && element.Name == "Synthetic unlock rejection.",
            cancellationToken);
        Assert.True(Assert.IsType<bool>(await automation.PropertyAsync(passwordField, "HasKeyboardFocus", cancellationToken)));
    }

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
