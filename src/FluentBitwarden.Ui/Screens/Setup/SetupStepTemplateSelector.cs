using FluentBitwarden.Ui.Screens.Setup.Steps.Email;
using FluentBitwarden.Ui.Screens.Setup.Steps.Password;
using FluentBitwarden.Ui.Screens.Setup.Steps.TwoFactor;

namespace FluentBitwarden.Ui.Screens.Setup;

public sealed partial class SetupStepTemplateSelector : DataTemplateSelector
{
    public DataTemplate? EmailTemplate { get; set; }
    public DataTemplate? PasswordTemplate { get; set; }
    public DataTemplate? TwoFactorTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item) => item switch
    {
        SetupEmailViewModel => EmailTemplate ?? throw new InvalidOperationException("Email template is not configured."),
        SetupPasswordViewModel => PasswordTemplate ?? throw new InvalidOperationException("Password template is not configured."),
        SetupTwoFactorViewModel => TwoFactorTemplate ?? throw new InvalidOperationException("Two-factor template is not configured."),
        _ => base.SelectTemplateCore(item)
    };

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) =>
        SelectTemplateCore(item);
}
