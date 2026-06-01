using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace FluentBitwarden.Shared.Controls;

[TemplatePart(Name = PartRingHost, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartRingProgress, Type = typeof(Path))]
[DependencyProperty<int>("Maximum", DefaultValue = 30)]
[DependencyProperty<int>("Value", DefaultValue = 0)]
[DependencyProperty<string>("ValueText", DefaultValue = "")]
[DependencyProperty<double>("RingThickness", DefaultValue = 4)]
[DependencyProperty<Brush>("RingBrush")]
[DependencyProperty<Brush>("RingTrackBrush")]
public sealed partial class CountdownRing : Control
{
    private const string PartRingHost = "PART_RingHost";
    private const string PartRingProgress = "PART_RingProgress";

    private FrameworkElement? _ringHost;
    private Path? _ringProgress;

    public CountdownRing()
    {
        DefaultStyleKey = typeof(CountdownRing);
    }

    partial void OnValueChanged()
    {
        ValueText = Value.ToString();
        UpdateRing();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _ringHost = GetTemplateChild(PartRingHost) as FrameworkElement;
        _ringProgress = GetTemplateChild(PartRingProgress) as Path;

        UpdateRing();
    }

    private void UpdateRing()
    {
        if (_ringHost is null || _ringProgress is null)
            return;

        double width = _ringHost.Width;
        double height = _ringHost.Height;

        if (width <= 0 || height <= 0)
            return;

        int value = Math.Clamp(Value, 0, Maximum);
        double percent = (double)value / Maximum;

        _ringProgress.Data = CreateArcGeometry(width, height, percent);
    }

    private Geometry? CreateArcGeometry(double width, double height, double percent)
    {
        if (percent <= 0)
            return null;

        double centerX = width / 2.0;
        double centerY = height / 2.0;
        double radius = Math.Min(width, height) / 2.0 - RingThickness / 2.0;

        if (radius <= 0)
            return null;

        if (percent >= 1.0)
        {
            return new EllipseGeometry
            {
                Center = new Point(centerX, centerY),
                RadiusX = radius,
                RadiusY = radius
            };
        }

        double startAngle = -90.0;
        double endAngle = startAngle + percent * 360.0;

        double startRadians = double.DegreesToRadians(startAngle);
        double endRadians = double.DegreesToRadians(endAngle);

        Point startPoint = new(
            centerX + radius * Math.Cos(startRadians),
            centerY + radius * Math.Sin(startRadians));

        Point endPoint = new(
            centerX + radius * Math.Cos(endRadians),
            centerY + radius * Math.Sin(endRadians));

        var figure = new PathFigure
        {
            StartPoint = startPoint,
            IsClosed = false,
            IsFilled = false
        };

        figure.Segments.Add(new ArcSegment
        {
            Point = endPoint,
            Size = new Size(radius, radius),
            SweepDirection = SweepDirection.Clockwise,
            IsLargeArc = percent > 0.5
        });

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);

        return geometry;
    }
}