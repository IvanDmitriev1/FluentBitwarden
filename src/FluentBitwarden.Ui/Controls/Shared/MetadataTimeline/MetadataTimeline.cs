using Microsoft.UI.Xaml;

namespace FluentBitwarden.Controls.Shared.MetadataTimeline;

[DependencyProperty<object>("Header")]
[DependencyProperty<DataTemplate>("HeaderTemplate")]
[DependencyProperty<string>("ToggleAutomationName")]
public sealed partial class MetadataTimeline : ItemsControl
{
    public MetadataTimeline()
    {
        DefaultStyleKey = typeof(MetadataTimeline);
    }
}
