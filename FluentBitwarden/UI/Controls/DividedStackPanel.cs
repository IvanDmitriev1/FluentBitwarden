using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI.Xaml.Shapes;

namespace FluentBitwarden.UI.Controls;

[DependencyProperty<Brush>("DividerBrush", DefaultBindingMode = DefaultBindingMode.OneTime)]
[DependencyProperty<double>("DividerThickness", DefaultValue = 1, DefaultBindingMode = DefaultBindingMode.OneTime)]
public sealed partial class DividedStackPanel : Panel
{
    private readonly List<Rectangle> _separators = [];

    private IEnumerable<UIElement> UserChildren =>
        Children.Where(c => !_separators.Contains(c));

    private List<UIElement> VisibleUserChildren =>
        UserChildren.Where(static c => c.Visibility == Visibility.Visible).ToList();

    private IEnumerable<UIElement> CollapsedUserChildren =>
        UserChildren.Where(static c => c.Visibility == Visibility.Collapsed);

    protected override Size MeasureOverride(Size availableSize)
    {
        var visible = VisibleUserChildren;
        SyncSeparators(visible.Count);

        double totalHeight = 0;
        double maxWidth = 0;

        foreach (var child in visible)
        {
            child.Measure(availableSize);
            totalHeight += child.DesiredSize.Height;
            maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
        }

        totalHeight += _separators.Count * DividerThickness;

        return new Size(maxWidth, totalHeight);
    }

    private void SyncSeparators(int count)
    {
        int needed = Math.Max(0, count - 1);

        while (_separators.Count > needed)
        {
            var last = _separators[^1];
            _separators.RemoveAt(_separators.Count - 1);
            Children.Remove(last);
        }

        while (_separators.Count < needed)
        {
            var sep = new Rectangle
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Height = DividerThickness,
                Fill = DividerBrush
            };

            _separators.Add(sep);
            Children.Add(sep);
        }

        foreach (var sep in _separators)
        {
            sep.Fill = DividerBrush;
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var visible = VisibleUserChildren;
        double y = 0;
        int sepIdx = 0;

        for (int i = 0; i < visible.Count; i++)
        {
            var child = visible[i];
            child.Arrange(new Rect(0, y, finalSize.Width, child.DesiredSize.Height));
            y += child.DesiredSize.Height;

            if (i < visible.Count - 1)
            {
                var sep = _separators[sepIdx++];
                sep.Arrange(new Rect(0, y, finalSize.Width, DividerThickness));
                y += DividerThickness;
            }
        }

        // Collapsed children still need to be arranged
        foreach (var child in CollapsedUserChildren)
        {
            child.Arrange(new Rect(0, 0, 0, 0));
        }

        return finalSize;
    }
}