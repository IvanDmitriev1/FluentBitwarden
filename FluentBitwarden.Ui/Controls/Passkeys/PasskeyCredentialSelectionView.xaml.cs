using Microsoft.UI.Xaml;

namespace FluentBitwarden.Controls.Passkeys;

public sealed partial class PasskeyCredentialSelectionView : UserControl
{ 
    public PasskeyCredentialSelectionView(IReadOnlyList<Fido2Credential> items)
    {
        InitializeComponent();
        Loaded += OnLoaded;

        _items = items;
    }

    private readonly TaskCompletionSource<Fido2Credential> _selectedCredential =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly IReadOnlyList<Fido2Credential> _items;

    public bool HasItems => _items.Count > 0;
    public bool HasNoItems => !HasItems;

    public Task<Fido2Credential> WaitUntilSelectedAsync(CancellationToken cancellationToken) =>
        _selectedCredential.Task.WaitAsync(cancellationToken);

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        ListView.ItemsSource = _items;
    }

    private void ListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListView.SelectedItem is Fido2Credential credential)
            _selectedCredential.TrySetResult(credential);
    }
}
