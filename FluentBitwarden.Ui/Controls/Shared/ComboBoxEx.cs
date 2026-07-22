using System.Collections;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;

namespace FluentBitwarden.Controls.Shared;

[TemplatePart(Name = PartComboBox, Type = typeof(ComboBox))]
[DependencyProperty<object>("ItemsSource")]
[DependencyProperty<object>("NoneItem")]
[DependencyProperty<object>("SelectedValue")]
[DependencyProperty<string>("SelectedValuePath", DefaultValue = "")]
[DependencyProperty<object>("Header")]
[DependencyProperty<DataTemplate>("ItemTemplate")]
public sealed partial class ComboBoxEx : Control
{
    private const string PartComboBox = "PART_ComboBox";

    private ComboBox? _comboBox;
    private INotifyCollectionChanged? _observedItems;
    private bool _isSyncing;

    public ComboBoxEx()
    {
        DefaultStyleKey = typeof(ComboBoxEx);
    }

    protected override void OnApplyTemplate()
    {
        _comboBox?.SelectionChanged -= OnComboBoxSelectionChanged;

        base.OnApplyTemplate();

        _comboBox = GetTemplateChild(PartComboBox) as ComboBox;

        _comboBox?.SelectionChanged += OnComboBoxSelectionChanged;

        Rebuild();
    }

    partial void OnItemsSourceChanged(object? oldValue, object? newValue)
    {
        _observedItems?.CollectionChanged -= OnItemsCollectionChanged;

        _observedItems = newValue as INotifyCollectionChanged;
        _observedItems?.CollectionChanged += OnItemsCollectionChanged;

        Rebuild();
    }

    partial void OnNoneItemChanged() => Rebuild();

    partial void OnSelectedValueChanged() => ApplySelection();

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void Rebuild()
    {
        if (_comboBox is null)
            return;

        var items = new List<object>();
        if (NoneItem is not null)
            items.Add(NoneItem);

        if (ItemsSource is IEnumerable source)
        {
            items.AddRange(source.Cast<object>());
        }

        _isSyncing = true;
        _comboBox.ItemsSource = items;
        _isSyncing = false;

        ApplySelection();
    }

    private void ApplySelection()
    {
        if (_comboBox is null)
            return;

        _isSyncing = true;

        _comboBox.SelectedValue = SelectedValue;

        // Any value that resolves to no real item (null/empty ids, deleted folder)
        // falls back to the None entry so it renders as the sentinel, not blank.
        if (_comboBox.SelectedIndex < 0)
            _comboBox.SelectedItem = NoneItem;

        _isSyncing = false;
    }

    private void OnComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncing || _comboBox is null)
            return;

        SelectedValue = ReferenceEquals(_comboBox.SelectedItem, NoneItem)
            ? null
            : _comboBox.SelectedValue;
    }
}
