using FluentBitwarden.Controls.Shared.SiteIcon;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Templates.Shared;

public partial class SiteIconTemplates : ResourceDictionary
{
    public SiteIconTemplates()
    {
        InitializeComponent();
    }
}

public sealed partial class SiteIconTemplateSelector : DataTemplateSelector
{
    public DataTemplate? FallbackTemplate { get; set; }
    public DataTemplate? ImageTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item) => item switch
    {
        SiteIconFallbackContent => FallbackTemplate,
        SiteIconImageContent => ImageTemplate,
        _ => base.SelectTemplateCore(item)
    };

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}
