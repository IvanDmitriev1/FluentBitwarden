using FluentBitwarden.Ui.Tests.TestInfrastructure;

namespace FluentBitwarden.Ui.Tests.Screens.Setup;

[Collection(DesktopUiCollection.Name)]
[Trait("Category", "Desktop")]
public sealed class SignInPageUiTests(DesktopUiFixture desktop)
{
    [Fact]
    public async Task RejectedPasswordDisplaysTheServerError()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await desktop.ResetToSignedOutAsync(cancellationToken);

        var automation = await desktop.AutomationAsync(cancellationToken);
        var emailInput = await automation.WaitAsync(
            UiSelectors.ById("SetupEmailTextBox"),
            "is enabled and visible",
            static element => element.IsEnabled && !element.IsOffscreen,
            cancellationToken);

        await automation.SetValueAsync(emailInput, TestCredentials.Email, cancellationToken);
        var emailContinue = await automation.WaitAsync(
            UiSelectors.ById("SetupContinueButton"),
            "is enabled and visible",
            static element => element.IsEnabled && !element.IsOffscreen,
            cancellationToken);

        await automation.InvokeAsync(emailContinue, cancellationToken);
        await automation.WaitAsync(
            UiSelectors.ById("SetupAccountEmail"),
            "name matches",
            element => element.Name == TestCredentials.Email,
            cancellationToken);

        var passwordInput = UiSelectors.ById("PasswordBoxInput", "SetupMasterPasswordBox");
        var passwordField = await automation.WaitAsync(
            passwordInput,
            "is enabled and visible",
            static element => element.IsEnabled && !element.IsOffscreen,
            cancellationToken);

        await automation.SetValueAsync(passwordField, TestCredentials.Password, cancellationToken);
        await automation.WaitAsync(
            passwordInput,
            "masked value matches",
            element => element.Value == new string('\u2022', TestCredentials.Password.Length),
            cancellationToken);
        var passwordContinue = await automation.WaitAsync(
            UiSelectors.ById("SetupPasswordContinueButton"),
            "is enabled and visible",
            static element => element.IsEnabled && !element.IsOffscreen,
            cancellationToken);

        await automation.InvokeAsync(passwordContinue, cancellationToken);
        await automation.WaitAsync(
            UiSelectors.ById("SetupPasswordError"),
            "visible name matches",
            element => !element.IsOffscreen && element.Name == UiTestComposition.Rejection,
            cancellationToken);
    }

    [Fact]
    public async Task GoingBackFromPasswordRestoresEmailAndClearsPasswordOnReentry()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await desktop.ResetToSignedOutAsync(cancellationToken);

        var automation = await desktop.AutomationAsync(cancellationToken);
        var emailInput = await automation.WaitAsync(
            UiSelectors.ById("SetupEmailTextBox"),
            "is enabled and visible",
            static element => element.IsEnabled && !element.IsOffscreen,
            cancellationToken);
        await automation.SetValueAsync(emailInput, TestCredentials.Email, cancellationToken);
        var emailContinue = await automation.WaitAsync(
            UiSelectors.ById("SetupContinueButton"),
            "is enabled and visible",
            static element => element.IsEnabled && !element.IsOffscreen,
            cancellationToken);
        await automation.InvokeAsync(emailContinue, cancellationToken);

        await automation.WaitAsync(
            UiSelectors.ById("SetupAccountEmail"),
            "name matches",
            element => element.Name == TestCredentials.Email,
            cancellationToken);
        var passwordInput = UiSelectors.ById("PasswordBoxInput", "SetupMasterPasswordBox");
        var passwordField = await automation.WaitAsync(
            passwordInput,
            "is enabled and visible",
            static element => element.IsEnabled && !element.IsOffscreen,
            cancellationToken);
        await automation.SetValueAsync(passwordField, TestCredentials.Password, cancellationToken);
        await automation.WaitAsync(
            passwordInput,
            "masked password matches",
            element => element.Value == new string('\u2022', TestCredentials.Password.Length),
            cancellationToken);

        var backButton = await automation.WaitAsync(
            UiSelectors.ById("Back to email and server"),
            "is enabled and visible",
            static element => element.IsEnabled && !element.IsOffscreen,
            cancellationToken);
        await automation.InvokeAsync(backButton, cancellationToken);
        await automation.WaitAsync(
            UiSelectors.ById("SetupEmailTextBox"),
            "email is restored",
            element => element.IsEnabled && !element.IsOffscreen && element.Value == TestCredentials.Email,
            cancellationToken);

        emailContinue = await automation.WaitAsync(
            UiSelectors.ById("SetupContinueButton"),
            "is enabled and visible",
            static element => element.IsEnabled && !element.IsOffscreen,
            cancellationToken);
        await automation.InvokeAsync(emailContinue, cancellationToken);
        await automation.WaitAsync(
            UiSelectors.ById("SetupAccountEmail"),
            "name matches",
            element => element.Name == TestCredentials.Email,
            cancellationToken);
        await automation.WaitAsync(
            passwordInput,
            "password is empty on reentry",
            static element => string.IsNullOrEmpty(element.Value),
            cancellationToken);
    }

    [Fact]
    public async Task AuthenticatorVerificationRequiresCodeAndShowsConfiguredRejection()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await desktop.ResetToSignedOutAsync(cancellationToken);
        desktop.ConfigureAuthenticatorRejection();

        var automation = await desktop.AutomationAsync(cancellationToken);
        var emailInput = await automation.WaitAsync(
            UiSelectors.ById("SetupEmailTextBox"),
            "is enabled and visible",
            static element => element.IsEnabled && !element.IsOffscreen,
            cancellationToken);
        await automation.SetValueAsync(emailInput, TestCredentials.Email, cancellationToken);
        var emailContinue = await automation.WaitAsync(
            UiSelectors.ById("SetupContinueButton"),
            "is enabled and visible",
            static element => element.IsEnabled && !element.IsOffscreen,
            cancellationToken);
        await automation.InvokeAsync(emailContinue, cancellationToken);
        await automation.WaitAsync(
            UiSelectors.ById("SetupAccountEmail"),
            "name matches",
            element => element.Name == TestCredentials.Email,
            cancellationToken);

        var passwordInput = UiSelectors.ById("PasswordBoxInput", "SetupMasterPasswordBox");
        var passwordField = await automation.WaitAsync(
            passwordInput,
            "is enabled and visible",
            static element => element.IsEnabled && !element.IsOffscreen,
            cancellationToken);
        await automation.SetValueAsync(passwordField, TestCredentials.Password, cancellationToken);
        var passwordContinue = await automation.WaitAsync(
            UiSelectors.ById("SetupPasswordContinueButton"),
            "is enabled and visible",
            static element => element.IsEnabled && !element.IsOffscreen,
            cancellationToken);
        await automation.InvokeAsync(passwordContinue, cancellationToken);

        var verifyButtonSelector = UiSelectors.ById("Verify code");
        await automation.WaitAsync(
            verifyButtonSelector,
            "disabled before a code is entered",
            static element => !element.IsOffscreen && !element.IsEnabled,
            cancellationToken);
        var codeInput = await automation.WaitAsync(
            UiSelectors.ById("SetupVerificationCodeTextBox"),
            "is enabled and visible",
            static element => element.IsEnabled && !element.IsOffscreen,
            cancellationToken);
        await automation.SetValueAsync(codeInput, "123456", cancellationToken);

        var verifyButton = await automation.WaitAsync(
            verifyButtonSelector,
            "enabled after a code is entered",
            static element => element.IsEnabled && !element.IsOffscreen,
            cancellationToken);
        await automation.InvokeAsync(verifyButton, cancellationToken);
        await automation.WaitAsync(
            UiSelectors.ById("SetupTwoFactorError"),
            "visible rejection matches",
            element => !element.IsOffscreen && element.Name == UiTestComposition.Rejection,
            cancellationToken);
    }
}
