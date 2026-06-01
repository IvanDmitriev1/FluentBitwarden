using Microsoft.UI.Xaml;

namespace FluentBitwarden.Shared.Controls;

[DependencyProperty<object>("Value")]
[DependencyProperty<object>("SelectedValue", DefaultBindingMode = DefaultBindingMode.TwoWay)]
public sealed partial class RadioMenuFlyoutItem : ToggleMenuFlyoutItem
{
    protected override void OnApplyTemplate()
    {
        Click -= OnClick;

        base.OnApplyTemplate();

        Click += OnClick;
    }

    partial void OnSelectedValueChanged()
    {
        var shouldBeChecked = Equals(Value, SelectedValue);

        if (IsChecked != shouldBeChecked)
            IsChecked = shouldBeChecked;
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        SelectedValue = Value;
    }
}