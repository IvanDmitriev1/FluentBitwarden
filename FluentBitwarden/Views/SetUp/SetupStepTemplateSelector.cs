using FluentBitwarden.ViewModels.SetUp;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Views.SetUp;

public sealed class SetupStepTemplateSelector : DataTemplateSelector
{
    public DataTemplate? PasswordSignInTemplate { get; set; }
    public DataTemplate? TwoFactorTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
        => item switch
        {
            PasswordSignInStepViewModel => PasswordSignInTemplate,
            TwoFactorStepViewModel => TwoFactorTemplate,
            _ => base.SelectTemplateCore(item),
        };

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
        => SelectTemplateCore(item);
}
