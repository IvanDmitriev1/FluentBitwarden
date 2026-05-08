using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Windows.Input;
using Windows.System;

namespace FluentBitwarden.Resources.Controls;

[TemplatePart(Name = PartIconPresenter, Type = typeof(ContentPresenter))]
[TemplatePart(Name = PartIconDivider, Type = typeof(Border))]
[DependencyProperty<ICommand>("Command")]
[DependencyProperty<IconElement>("Icon")]
public sealed partial class TextBoxEx : TextBox
{
    private const string PartIconPresenter = "PART_IconPresenter";
    private const string PartIconDivider = "PART_IconDivider";

    private ContentControl? _iconPresenter;
    private Border? _iconDivider;

    public TextBoxEx()
    {
        DefaultStyleKey = typeof(TextBoxEx);
    }

    partial void OnIconChanged()
    {
        if (_iconPresenter is null || _iconDivider is null)
            return;

        Visibility visibility = Icon is null ? Visibility.Collapsed : Visibility.Visible;
        _iconPresenter.Visibility = visibility;
        _iconDivider.Visibility = visibility;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _iconPresenter = GetTemplateChild(PartIconPresenter) as ContentControl;
        _iconDivider = GetTemplateChild(PartIconDivider) as Border;

        OnIconChanged();
    }

    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && Command?.CanExecute(null) == true)
        {
            Command.Execute(null);
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }
}
