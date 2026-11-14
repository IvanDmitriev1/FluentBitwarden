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
        if (sender is not ListView { SelectedItem: Cipher selected } listView)
            return;

        if (!listView.IsLoaded || !ReferenceEquals(listView.SelectedItem, selected))
            return;

        listView.SmoothScrollIntoViewWithItemAsync(selected).SafeFireAndForget();
    }
}
