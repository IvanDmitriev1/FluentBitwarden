using System.Collections.ObjectModel;
using BitwardenApi.Vault.Items.Contracts;
using FluentBitwarden.Controls.Shared;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace FluentBitwarden.AttachedProperties;

/// <summary>
/// Adds/removes rows in a <see cref="DividedCardItemsControl"/> bound to a plain <see cref="List{T}"/>
/// of <see cref="LoginUri"/>. The list itself raises no change notification, so <see cref="Source"/>
/// wraps it in an <see cref="ObservableCollection{T}"/> for the card's ItemsSource and mirrors every
/// change back into the original list. <see cref="Target"/> attaches to an add/remove button pointed at
/// the card: clicking adds a blank row, or removes the button's own row when its DataContext is a
/// <see cref="LoginUri"/>.
/// </summary>
[AttachedDependencyProperty<List<LoginUri>>("Source")]
[AttachedDependencyProperty<DividedCardItemsControl>("Target")]
public static partial class UriListEditBehavior
{
    static partial void OnSourceChanged(DependencyObject dependencyObject, List<LoginUri>? newValue)
    {
        if (dependencyObject is not DividedCardItemsControl card)
            return;

        var source = newValue ?? [];
        var collection = new ObservableCollection<LoginUri>(source);
        collection.CollectionChanged += (_, _) =>
        {
            source.Clear();
            source.AddRange(collection);
        };

        card.ItemsSource = collection;
    }

    static partial void OnTargetChanged(DependencyObject dependencyObject, DividedCardItemsControl? newValue)
    {
        if (dependencyObject is not ButtonBase button)
            return;

        button.Click -= OnClick;
        if (newValue is not null)
            button.Click += OnClick;
    }

    private static void OnClick(object sender, RoutedEventArgs e)
    {
        var button = (ButtonBase)sender;
        if (GetTarget(button)?.ItemsSource is not ObservableCollection<LoginUri> collection)
            return;

        if (button.DataContext is LoginUri uri)
            collection.Remove(uri);
        else
            collection.Add(new LoginUri());
    }
}
