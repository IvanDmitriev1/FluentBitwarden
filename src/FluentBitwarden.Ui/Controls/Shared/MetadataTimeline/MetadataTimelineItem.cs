namespace FluentBitwarden.Controls.Shared.MetadataTimeline;

[DependencyProperty<string>("Text", DefaultValue = "")]
[DependencyProperty<string>("Glyph", DefaultValue = "")]
public sealed partial class MetadataTimelineItem : Control
{
    public MetadataTimelineItem()
    {
        DefaultStyleKey = typeof(MetadataTimelineItem);
    }
}
