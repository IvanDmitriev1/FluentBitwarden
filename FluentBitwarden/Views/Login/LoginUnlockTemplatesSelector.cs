using FluentBitwarden.ViewModels.Login;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Views.Login;

public sealed class LoginUnlockTemplatesSelector : DataTemplateSelector
{
    public DataTemplate? MasterPasswordUnlockViewModelTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
        => item switch
        {
            MasterPasswordUnlockViewModel => MasterPasswordUnlockViewModelTemplate,
            _ => base.SelectTemplateCore(item),
        };

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
        => SelectTemplateCore(item);
}