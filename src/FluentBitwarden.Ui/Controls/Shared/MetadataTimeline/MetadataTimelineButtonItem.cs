namespace FluentBitwarden.Controls.Shared.MetadataTimeline;

[DependencyProperty<string>("Text", DefaultValue = "")]
[DependencyProperty<string>("Glyph", DefaultValue = "")]
public sealed partial class MetadataTimelineButtonItem : Button
{
    public MetadataTimelineButtonItem()
    {
        DefaultStyleKey = typeof(MetadataTimelineButtonItem);
    }
}
