using FluentBitwarden.Ui.Screens.Loading;
using FluentBitwarden.Ui.Screens.Setup;
using FluentBitwarden.Ui.Screens.Shell;
using FluentBitwarden.Ui.Screens.Unlock;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentBitwarden.Ui.Screens;

public sealed partial class RootTemplates : ResourceDictionary
{
    public RootTemplates() => InitializeComponent();
}

public sealed partial class RootTemplateSelector : DataTemplateSelector
{
    public DataTemplate LoadingTemplate { get; set; } = null!;
    public DataTemplate SetupTemplate { get; set; } = null!;
    public DataTemplate UnlockTemplate { get; set; } = null!;
    public DataTemplate ShellTemplate { get; set; } = null!;

    protected override DataTemplate SelectTemplateCore(object item) => item switch
    {
        LoadingViewModel => LoadingTemplate,
        SetupViewModel => SetupTemplate,
        UnlockViewModel => UnlockTemplate,
        ShellViewModel => ShellTemplate,
        _ => base.SelectTemplateCore(item)
    };

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) =>
        SelectTemplateCore(item);
}
