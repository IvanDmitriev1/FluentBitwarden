using FluentBitwarden.ViewModels.Setup;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Views.Setup;

public sealed class SetupStepTemplateSelector : DataTemplateSelector
{
    public DataTemplate? EmailSignInTemplate { get; set; }
    public DataTemplate? PasswordSignInTemplate { get; set; }
    public DataTemplate? TwoFactorTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
        => item switch
        {
            EmailSignInStepState => EmailSignInTemplate,
            PasswordSignInStepState => PasswordSignInTemplate,
            TwoFactorStepState => TwoFactorTemplate,
            _ => base.SelectTemplateCore(item),
        };

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
        => SelectTemplateCore(item);
}
