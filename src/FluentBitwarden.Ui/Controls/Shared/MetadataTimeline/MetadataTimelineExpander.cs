using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;

namespace FluentBitwarden.Controls.Shared.MetadataTimeline;

[WinRT.GeneratedBindableCustomProperty(["IsExpanded"], [])]
public sealed partial class MetadataTimelineExpander : Expander
{
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (GetTemplateChild("ExpanderHeader") is ToggleButton headerButton)
        {
            headerButton.SetBinding(
                ToggleButton.IsCheckedProperty,
                new Binding
                {
                    Source = this,
                    Path = new PropertyPath(nameof(IsExpanded)),
                    Mode = BindingMode.TwoWay
                });
        }
    }
}
