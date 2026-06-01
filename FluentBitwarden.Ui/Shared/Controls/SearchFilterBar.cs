using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace FluentBitwarden.Shared.Controls;


[TemplatePart(Name = PartSearchBox, Type = typeof(TextBox))]
[TemplatePart(Name = PartSearchToggleButton, Type = typeof(Button))]
[TemplateVisualState(Name = StateNormal, GroupName = GroupSearchStates)]
[TemplateVisualState(Name = StateSearchOpen, GroupName = GroupSearchStates)]
[DependencyProperty<string>("SearchText", DefaultValue = "", DefaultBindingMode = DefaultBindingMode.TwoWay)]
[DependencyProperty<bool>("IsSearchOpen", DefaultValue = false, DefaultBindingMode = DefaultBindingMode.TwoWay)]
[DependencyProperty<string>("SearchPlaceholderText", DefaultValue = "Search")]
[DependencyProperty<object>("FilterContent", DefaultValue = null)]
[DependencyProperty<DataTemplate>("FilterContentTemplate")]
[DependencyProperty<FlyoutBase>("SortFlyout")]
[DependencyProperty<string>("SearchButtonToolTip", DefaultValue = "Search")]
[DependencyProperty<string>("CloseButtonToolTip", DefaultValue = "Close search")]
[DependencyProperty<string>("SortButtonToolTip", DefaultValue = "Sort items")]
public sealed partial class SearchFilterBar : Control
{
    private const string PartSearchBox = "PART_SearchBox";
    private const string PartSearchToggleButton = "PART_SearchToggleButton";

    private const string GroupSearchStates = "SearchStates";
    private const string StateNormal = "Normal";
    private const string StateSearchOpen = "SearchOpen";

    private TextBox? _searchBox;
    private Button? _searchToggleButton;
    private bool _isTemplateApplied;

    public SearchFilterBar()
    {
        DefaultStyleKey = typeof(SearchFilterBar);
    }

    partial void OnIsSearchOpenChanged()
    {
        if (!_isTemplateApplied)
            return;

        UpdateSearchState();
    }

    protected override void OnApplyTemplate()
    {
        _searchBox?.KeyDown -= SearchBoxOnKeyDown;
        _searchToggleButton?.Click -= OnSearchToggleButtonClick;

        base.OnApplyTemplate();

        _searchBox = GetTemplateChild(PartSearchBox) as TextBox;
        _searchToggleButton = GetTemplateChild(PartSearchToggleButton) as Button;

        _searchBox?.KeyDown += SearchBoxOnKeyDown;
        _searchToggleButton?.Click += OnSearchToggleButtonClick;

        _isTemplateApplied = true;
        UpdateSearchState(useTransitions: false);
    }

    private void SearchBoxOnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Escape)
            return;

        e.Handled = true;
        IsSearchOpen = false;
    }

    private void OnSearchToggleButtonClick(object sender, RoutedEventArgs e) =>
        IsSearchOpen = !IsSearchOpen;

    private void UpdateSearchState(bool useTransitions = true)
    {
        VisualStateManager.GoToState(
            this,
            IsSearchOpen ? StateSearchOpen : StateNormal,
            useTransitions);

        ToolTipService.SetToolTip(
            _searchToggleButton,
            IsSearchOpen ? CloseButtonToolTip : SearchButtonToolTip);

        if (!IsSearchOpen)
        {
            SearchText = string.Empty;
        }
        else
        {
            _searchBox?.Focus(FocusState.Programmatic);
        }
    }
}
