namespace FluentBitwarden.Ui.Screens.Setup;

public interface ISetupNavigator
{
    void ShowPassword(SetupPasswordParameters parameters);
    void ShowTwoFactor(SetupTwoFactorParameters parameters);
    void GoBackToEmail(SetupEmailParameters parameters);
}
