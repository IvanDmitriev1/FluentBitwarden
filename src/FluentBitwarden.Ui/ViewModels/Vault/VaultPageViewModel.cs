using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Contracts.Modules.Vault;
using FluentBitwarden.Contracts.Modules.Vault.Synchronization;

namespace FluentBitwarden.ViewModels.Vault;

public sealed partial class VaultPageViewModel(
    IVaultClient vaultClient) : ObservableObject, INavigationAware, INavigationAware<VaultCipherNavigationIntent>
{
    [ObservableProperty]
    public partial VaultCipher? SelectedCipher { get; set; }

    [ObservableProperty]
    public partial VaultCipher? EditingCipher { get; set; }

    [ObservableProperty]
    public partial bool IsEditing { get; private set; }

    [ObservableProperty]
    public partial VaultCipherQuery CurrentQuery { get; set; } = VaultCipherQuery.QueryAll;

    [ObservableProperty]
    public partial CipherId RequestedCipherId { get; set; } = CipherId.Empty;

    [ObservableProperty]
    public partial ObservableCollection<VaultFolder> Folders { get; set; } = [];

    private bool _hasInitialized;

    partial void OnSelectedCipherChanged(VaultCipher? value)
    {
        var composite = value is null
            ? VaultBrowseState.Default
            : new VaultBrowseState(CurrentQuery.SearchText, value.Id);

        SettingsStore.Instance.SetComposite(UiSettingKeys.Vault.StateKey, composite);

        EditingCipher = value;
        CancelEditCipher();
    }

    public ValueTask OnNavigatedToAsync(CancellationToken cancellationToken) =>
        new(EnsureLoadedAsync(cancellationToken));

    public async ValueTask OnNavigatedToAsync(
        VaultCipherNavigationIntent param,
        CancellationToken cancellationToken)
    {
        switch (param)
        {
            case ShowVaultCipherIntent show:
                await LoadOrApplyNavigationAsync(show, cancellationToken);
                break;
            case OpenVaultCipherIntent open:
                var cipher = await vaultClient.GetCipherAsync(
                    new GetVaultCipherRequest(open.CipherId),
                    cancellationToken);

                if (cipher is null)
                {
                    await EnsureLoadedAsync(cancellationToken);
                    break;
                }

                await LoadOrApplyNavigationAsync(
                    new ShowVaultCipherIntent(string.Empty, cipher),
                    cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(param), param, null);
        }
    }

    public ValueTask OnNavigatedFromAsync(CancellationToken cancellationToken) =>
        ValueTask.CompletedTask;

    internal void ApplyNavigationIntent(ShowVaultCipherIntent intent)
    {
        CurrentQuery = new VaultCipherQuery { SearchText = intent.SearchText, CipherType = null };
        RequestedCipherId = intent.SelectedCipher.Id;
    }

    [RelayCommand]
    private void BeginEditCipher()
    {
        if (SelectedCipher is null)
            return;

        EditingCipher = SelectedCipher;
        IsEditing = true;
    }

    [RelayCommand]
    private void BeginCreateCipher(VaultCipherType type)
    {
        EditingCipher = VaultCipher.CreateBlankCipher(type);
        IsEditing = true;
    }

    [RelayCommand]
    private void CancelEditCipher()
    {
        IsEditing = false;

        if (EditingCipher?.Id == CipherId.Empty)
        {
            SelectedCipher = null;
            EditingCipher = null;
        }
        else
        {
            EditingCipher = SelectedCipher;
        }
    }

    [RelayCommand]
    private async Task SaveCipher(CancellationToken cancellationToken)
    {
        if (EditingCipher is null)
            return;

        var saved = await vaultClient.SaveCipherAsync(new SaveVaultCipherRequest(EditingCipher), cancellationToken);
        if (saved is null)
            return; // Save failed; stay in edit mode so the user can retry.

        IsEditing = false;
        SelectedCipher = saved;
        RequestedCipherId = saved.Id;
    }

    private Task LoadOrApplyNavigationAsync(
        ShowVaultCipherIntent intent,
        CancellationToken cancellationToken) => _hasInitialized
        ? ApplyNavigationAsync(intent, cancellationToken)
        : EnsureLoadedAsync(cancellationToken, intent);

    private Task ApplyNavigationAsync(ShowVaultCipherIntent message, CancellationToken cancellationToken)
    {
        ApplyNavigationIntent(message);
        return Task.CompletedTask;
    }

    private async Task EnsureLoadedAsync(
        CancellationToken cancellationToken,
        ShowVaultCipherIntent? initialNavigation = null)
    {
        if (_hasInitialized)
            return;

        var state = SettingsStore.Instance.GetComposite(UiSettingKeys.Vault.StateKey);

        if (initialNavigation is null)
        {
            CurrentQuery = new VaultCipherQuery { SearchText = state.SearchText, CipherType = null };
            RequestedCipherId = state.SelectedCipherId;
        }
        else
        {
            CurrentQuery = new VaultCipherQuery { SearchText = initialNavigation.SearchText, CipherType = null };
            RequestedCipherId = initialNavigation.SelectedCipher.Id;
        }

        await SyncVault(cancellationToken);
        _hasInitialized = true;
    }

    private async Task SyncVault(CancellationToken cancellationToken)
    {
        var result = await vaultClient.SyncVaultAsync(cancellationToken);
        Folders = new ObservableCollection<VaultFolder>(await vaultClient.GetFoldersAsync(cancellationToken));

        if (result != VaultSyncResult.Synced)
            return;

        RequestedCipherId = SelectedCipher?.Id ?? CipherId.Empty;
    }
}
