namespace FluentBitwarden.ViewModels.Passkeys;

public sealed partial class PasskeySelectPageViewModel : ObservableObject
{
    private readonly TaskCompletionSource<Fido2Credential> _selectedCredential = new(
        TaskCreationOptions.RunContinuationsAsynchronously);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasItems))]
    [NotifyPropertyChangedFor(nameof(HasNoItems))]
    public partial IReadOnlyList<Fido2Credential> Items { get; set; } = [];

    [ObservableProperty]
    public partial Fido2Credential? SelectedItem { get; set; }

    public bool HasItems => Items.Count > 0;
    public bool HasNoItems => !HasItems;

    public Task<Fido2Credential> WaitUntilSelectedAsync(CancellationToken cancellationToken) =>
        _selectedCredential.Task.WaitAsync(cancellationToken);

    partial void OnSelectedItemChanged(Fido2Credential? value)
    {
        if (value is not null)
        {
            _selectedCredential.TrySetResult(value);
        }
    }
}
