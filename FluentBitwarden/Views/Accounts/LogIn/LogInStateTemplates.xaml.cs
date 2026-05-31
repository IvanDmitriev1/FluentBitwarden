using FluentBitwarden.Views.Accounts.LogIn.States;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Views.Accounts.LogIn;

public partial class LogInStateTemplates : ResourceDictionary
{
    public LogInStateTemplates()
    {
        InitializeComponent();
    }
}

public sealed class LogInStateTemplatesSelector : DataTemplateSelector
{
    public DataTemplate? EmailLogInTemplate { get; set; }
    public DataTemplate? PasswordLogInTemplate { get; set; }
    public DataTemplate? TwoFactorTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object? item)
        => item switch
        {
            LogInEmailStepViewModel => EmailLogInTemplate,
            LogInPasswordStepViewModel => PasswordLogInTemplate,
            LogIn2FStepViewModel => TwoFactorTemplate,
            _ => base.SelectTemplateCore(item)
        };

    protected override DataTemplate? SelectTemplateCore(object? item, DependencyObject container)
        => SelectTemplateCore(item);
}