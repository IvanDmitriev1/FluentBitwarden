using System.Globalization;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace FluentBitwarden.Ui.Controls;

[DependencyProperty<int>("Maximum", DefaultValue = 100)]
[DependencyProperty<int>("Value", DefaultValue = 0)]
[DependencyProperty<int>("WarningAt", DefaultValue = -1)]
[DependencyProperty<Brush>("NormalRingBrush")]
[DependencyProperty<Brush>("WarningRingBrush")]
public sealed partial class CountdownRing : Control
{
    private Path? _arc;
    private TextBlock? _label;

    public CountdownRing() => DefaultStyleKey = typeof(CountdownRing);

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _arc = GetTemplateChild("PART_Arc") as Path;
        _label = GetTemplateChild("PART_Label") as TextBlock;

        UpdateDisplay();
    }

    partial void OnMaximumChanged() => UpdateDisplay();
    partial void OnValueChanged() => UpdateDisplay();
    partial void OnWarningAtChanged() => UpdateDisplay();
    partial void OnNormalRingBrushChanged() => UpdateDisplay();
    partial void OnWarningRingBrushChanged() => UpdateDisplay();

    private void UpdateDisplay()
    {
        int maximum = Math.Max(0, Maximum);
        int value = Math.Clamp(Value, 0, maximum);
        int cutoff = WarningAt < 0 ? maximum / 3 : WarningAt;

        if (_arc is not null)
        {
            _arc.Data = CreateArc(value, maximum);
            _arc.Stroke = value <= cutoff ? WarningRingBrush : NormalRingBrush;
        }

        _label?.Text = value.ToString("D2", CultureInfo.CurrentCulture);
    }

    private static Geometry? CreateArc(int value, int maximum)
    {
        const double center = 7;
        const double radius = 5.5;

        if (value == 0)
            return null;

        if (value == maximum)
            return new EllipseGeometry { Center = new Point(center, center), RadiusX = radius, RadiusY = radius };

        double fraction = (double)value / maximum;
        double angle = fraction * Math.Tau;
        var figure = new PathFigure { StartPoint = new Point(center, center - radius) };
        figure.Segments.Add(new ArcSegment
        {
            Point = new Point(center + radius * Math.Sin(angle), center - radius * Math.Cos(angle)),
            Size = new Size(radius, radius),
            IsLargeArc = fraction > 0.5,
            SweepDirection = SweepDirection.Clockwise,
        });

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }
}
