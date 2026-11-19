using FluentBitwarden.Ui.Controls;
using FluentBitwarden.Ui.Tests.TestInfrastructure;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace FluentBitwarden.Ui.Tests.Controls;

[Collection(DesktopUiCollection.Name)]
public sealed class CountdownRingTests(DesktopUiFixture desktop)
{
    [Fact]
    public async Task AppliedTemplateTracksValuesAndWarningCutoff()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var loaded = new TaskCompletionSource<CountdownRing>(TaskCreationOptions.RunContinuationsAsynchronously);
        Window? window = null;

        try
        {
            await desktop.RunOnUiThreadAsync(() =>
            {
                var control = new CountdownRing
                {
                    Maximum = 30,
                    Value = 11,
                    NormalRingBrush = new SolidColorBrush(Colors.Green),
                    WarningRingBrush = new SolidColorBrush(Colors.Orange),
                };

                control.Loaded += (_, _) => loaded.TrySetResult(control);
                window = new Window { Content = control };
                window.Activate();
            }, cancellationToken);

            var control = await loaded.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            await desktop.RunOnUiThreadAsync(() => VerifyCountdown(control), cancellationToken);
        }
        finally
        {
            await desktop.RunOnUiThreadAsync(() => window?.Close(), CancellationToken.None);
        }
    }

    private static void VerifyCountdown(CountdownRing control)
    {
        var pill = Assert.IsType<Border>(VisualTreeHelper.GetChild(control, 0));
        var contents = Assert.IsType<StackPanel>(pill.Child);
        var ring = Assert.IsType<Grid>(contents.Children[0]);
        var track = Assert.IsType<Ellipse>(ring.Children[0]);
        var arc = Assert.IsType<Path>(ring.Children[1]);
        var label = Assert.IsType<TextBlock>(contents.Children[1]);
        Assert.Equal(3d, track.StrokeThickness);
        Assert.Equal(track.StrokeThickness, arc.StrokeThickness);
        Assert.Equal("11", label.Text);
        Assert.Same(control.NormalRingBrush, arc.Stroke);

        control.Value = 10;
        Assert.Same(control.WarningRingBrush, arc.Stroke);

        control.Value = 3;
        var geometry = Assert.IsType<PathGeometry>(arc.Data);
        var segment = Assert.IsType<ArcSegment>(Assert.Single(Assert.Single(geometry.Figures).Segments));
        Assert.Equal(10.23, segment.Point.X, precision: 2);
        Assert.Equal(2.55, segment.Point.Y, precision: 2);
        Assert.Equal(SweepDirection.Clockwise, segment.SweepDirection);
        Assert.Equal("03", label.Text);
        Assert.Same(control.WarningRingBrush, arc.Stroke);

        control.WarningAt = 0;
        Assert.Same(control.NormalRingBrush, arc.Stroke);
        control.Value = -1;
        Assert.Null(arc.Data);
        Assert.Equal("00", label.Text);
        Assert.Same(control.WarningRingBrush, arc.Stroke);

        control.Maximum = 0;
        control.Value = 40;
        Assert.Null(arc.Data);
        Assert.Equal(40, control.Value);
        control.Maximum = 30;
        Assert.IsType<EllipseGeometry>(arc.Data);
        Assert.Equal("30", label.Text);
        Assert.Same(control.NormalRingBrush, arc.Stroke);
    }
}
