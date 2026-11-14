using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace FluentBitwarden.Ui.Controls;

[TemplatePart(Name = PartSearchBox, Type = typeof(TextBox))]
[TemplatePart(Name = PartFilterButton, Type = typeof(DropDownButton))]
[TemplatePart(Name = PartSearchToggleButton, Type = typeof(Button))]
[TemplatePart(Name = PartSortButton, Type = typeof(Button))]
[TemplateVisualState(Name = StateNormal, GroupName = GroupSearchStates)]
[TemplateVisualState(Name = StateSearchOpen, GroupName = GroupSearchStates)]
[DependencyProperty<object>("FilterContent")]
[DependencyProperty<FlyoutBase>("FilterFlyout")]
[DependencyProperty<FlyoutBase>("SortFlyout")]
[DependencyProperty<string>("SearchText", DefaultValue = "")]
[DependencyProperty<bool>("IsSearchOpen")]
[DependencyProperty<string>("SearchPlaceholderText", DefaultValue = "")]
[DependencyProperty<string>("FilterToolTip")]
[DependencyProperty<string>("OpenSearchToolTip")]
[DependencyProperty<string>("CloseSearchToolTip")]
[DependencyProperty<string>("SortToolTip")]
public sealed partial class ListQueryBar : Control
{
    private const string PartSearchBox = "PART_SearchBox";
    private const string PartFilterButton = "PART_FilterButton";
    private const string PartSearchToggleButton = "PART_SearchToggleButton";
    private const string PartSortButton = "PART_SortButton";

    private const string GroupSearchStates = "SearchStates";
    private const string StateNormal = "Normal";
    private const string StateSearchOpen = "SearchOpen";

    private TextBox? _searchBox;
    private DropDownButton? _filterButton;
    private Button? _searchToggleButton;
    private Button? _sortButton;
    private bool _isTemplateApplied;


    public ListQueryBar()
    {
        DefaultStyleKey = typeof(ListQueryBar);
    }

    partial void OnFilterFlyoutChanged(FlyoutBase? oldValue, FlyoutBase? newValue)
    {
        UpdateFlyoutVisibility();
        if (oldValue is MenuFlyout oldMenu)
        {
            foreach (var item in oldMenu.Items.OfType<RadioMenuFlyoutItem>())
                item.Click -= OnFilterItemClick;
        }

        if (newValue is MenuFlyout newMenu)
        {
            foreach (var item in newMenu.Items.OfType<RadioMenuFlyoutItem>())
            {
                item.Click += OnFilterItemClick;
                if (FilterContent is null && item.IsChecked)
                    FilterContent = CreateFilterContent(item);
            }
        }
    }

    partial void OnSortFlyoutChanged() => UpdateFlyoutVisibility();

    partial void OnIsSearchOpenChanged()
    {
        if (!_isTemplateApplied)
            return;

        UpdateSearchState(useTransitions: true);
    }

    partial void OnSearchTextChanged(string oldValue, string newValue)
    {
        if (_searchBox?.Text == newValue)
            return;

        if (!string.IsNullOrEmpty(newValue) || !string.IsNullOrEmpty(oldValue))
            IsSearchOpen = !string.IsNullOrEmpty(newValue);

        SyncSearchBoxText();
    }

    protected override void OnApplyTemplate()
    {
        _searchBox?.KeyDown -= SearchBoxOnKeyDown;
        _searchBox?.TextChanged -= SearchBoxOnTextChanged;
        _searchToggleButton?.Click -= OnSearchToggleButtonClick;

        base.OnApplyTemplate();

        _searchBox = GetTemplateChild(PartSearchBox) as TextBox;
        _filterButton = GetTemplateChild(PartFilterButton) as DropDownButton;
        _searchToggleButton = GetTemplateChild(PartSearchToggleButton) as Button;
        _sortButton = GetTemplateChild(PartSortButton) as Button;

        _searchBox?.KeyDown += SearchBoxOnKeyDown;
        _searchBox?.TextChanged += SearchBoxOnTextChanged;
        _searchToggleButton?.Click += OnSearchToggleButtonClick;

        _isTemplateApplied = true;
        SyncSearchBoxText();
        UpdateFlyoutVisibility();
        UpdateSearchState(useTransitions: false);
    }

    private void OnSearchToggleButtonClick(object sender, RoutedEventArgs e)
    {
        if (IsSearchOpen)
            CloseSearch();
        else
            OpenSearch();
    }

    private void SearchBoxOnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Escape)
            return;

        e.Handled = true;
        CloseSearch();
    }

    private void SyncSearchBoxText()
    {
        _searchBox?.Text = SearchText ?? string.Empty;
    }

    private void OpenSearch()
    {
        IsSearchOpen = true;
        _searchBox?.Focus(FocusState.Programmatic);
    }

    private void CloseSearch()
    {
        IsSearchOpen = false;
        _searchToggleButton?.Focus(FocusState.Programmatic);

        if (!string.IsNullOrEmpty(SearchText))
        {
            SearchText = string.Empty;
        }
    }

    private void SearchBoxOnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox && (SearchText ?? string.Empty) != textBox.Text)
        {
            SearchText = textBox.Text;
        }
    }

    private void UpdateSearchState(bool useTransitions)
    {
        VisualStateManager.GoToState(
            this,
            IsSearchOpen ? StateSearchOpen : StateNormal,
            useTransitions);

        if (_searchToggleButton is not null)
        {
            string? automationName = IsSearchOpen ? CloseSearchToolTip : OpenSearchToolTip;
            ToolTipService.SetToolTip(_searchToggleButton, automationName);
            AutomationProperties.SetName(_searchToggleButton, automationName);
        }
    }

    private void UpdateFlyoutVisibility()
    {
        _filterButton?.Visibility = FilterFlyout is null ?
            Visibility.Collapsed:
            Visibility.Visible;

        _sortButton?.Visibility = SortFlyout is null ?
            Visibility.Collapsed :
            Visibility.Visible;
    }

    private void OnFilterItemClick(object sender, RoutedEventArgs args)
    {
        if (sender is not RadioMenuFlyoutItem item)
            return;

        FilterContent = CreateFilterContent(item);
    }

    private static StackPanel CreateFilterContent(RadioMenuFlyoutItem item)
    {
        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        if (item.Icon is FontIcon icon)
        {
            content.Children.Add(new FontIcon
            {
                Glyph = icon.Glyph,
                FontFamily = icon.FontFamily
            });
        }

        content.Children.Add(new TextBlock
        {
            Text = item.Text,
            VerticalAlignment = VerticalAlignment.Center
        });

        return content;
    }
}
