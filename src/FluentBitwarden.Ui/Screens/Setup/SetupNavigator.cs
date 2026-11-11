using FluentBitwarden.Ui.Screens.Setup.Steps.Email;
using FluentBitwarden.Ui.Screens.Setup.Steps.Password;
using FluentBitwarden.Ui.Screens.Setup.Steps.TwoFactor;
using Navis.WinUI.Abstractions;

namespace FluentBitwarden.Ui.Screens.Setup;

internal sealed class SetupNavigator(INavigation navigation) : ISetupNavigator
{
    public void ShowPassword(SetupPasswordParameters parameters) =>
        navigation.Navigate<SetupPasswordPage, SetupPasswordParameters>(parameters, NavigationKind.Reset);

    public void ShowTwoFactor(SetupTwoFactorParameters parameters) =>
        navigation.Navigate<SetupTwoFactorPage, SetupTwoFactorParameters>(parameters, NavigationKind.Reset);

    public void GoBackToEmail(SetupEmailParameters parameters) =>
        navigation.Navigate<SetupEmailPage, SetupEmailParameters>(parameters, NavigationKind.Reset);
}
