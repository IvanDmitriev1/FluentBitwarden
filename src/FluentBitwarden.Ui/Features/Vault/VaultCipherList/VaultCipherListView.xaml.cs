using System.Windows.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Input;
using VirtualKey = Windows.System.VirtualKey;

namespace FluentBitwarden.Ui.Features.Vault.VaultCipherList;

[DependencyProperty<IEnumerable<Cipher>>("ItemsSource")]
[DependencyProperty<Cipher>("SelectedCipher")]
[DependencyProperty<string>("SearchText", DefaultValue = "")]
[DependencyProperty<ICommand>("CategoryChangedCommand")]
[DependencyProperty<ICommand>("SortChangedCommand")]
public sealed partial class VaultCipherListView : UserControl
{
    public VaultCipherListView()
    {
        InitializeComponent();
    }

    private void CipherList_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (sender is not ListView listView)
            return;

        if (e.Key == VirtualKey.Escape && listView.SelectedItem is not null)
        {
            listView.SelectedItem = null;
            e.Handled = true;
        }
    }

    private void CipherList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView && !ReferenceEquals(SelectedCipher, listView.SelectedItem))
            SelectedCipher = listView.SelectedItem as Cipher;
    }

    partial void OnSelectedCipherChanged()
    {
        var listView = CipherList;
        var selected = SelectedCipher;
        if (ReferenceEquals(listView.SelectedItem, selected))
            return;

        listView.SelectedItem = selected;
        if (listView.IsLoaded && selected is not null && ReferenceEquals(listView.SelectedItem, selected))
            ScrollAndFocusAsync(listView, selected).SafeFireAndForget();
    }

    private static Task ScrollAndFocusAsync(ListView listView, Cipher selected)
    {
        if (listView.ContainerFromItem(selected) is null)
        {
            listView.ScrollIntoView(selected);
            listView.UpdateLayout();
        }

        if (ReferenceEquals(listView.SelectedItem, selected) &&
            listView.ContainerFromItem(selected) is ListViewItem row)
        {
            row.Focus(FocusState.Keyboard);
        }

        return listView.SmoothScrollIntoViewWithItemAsync(
            selected, ScrollItemPlacement.Center);
    }

}
