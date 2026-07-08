using Microsoft.UI.Xaml;

namespace FluentBitwarden.Controls.Shared;

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

    partial void OnSelectedValueChanged() => UpdateIsChecked();
    partial void OnValueChanged() => UpdateIsChecked();

    private void UpdateIsChecked()
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
