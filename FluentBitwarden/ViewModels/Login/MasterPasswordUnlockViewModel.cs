using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Models;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.ViewModels;

internal partial class MasterPasswordUnlockViewModel : ObservableObject
{
    private readonly IVaultService _vaultService;

    public MasterPasswordUnlockViewModel(
        LoginPageViewModel parentViewModel,
        IVaultService vaultService)
    {
        ParentViewModel = parentViewModel;
        _vaultService = vaultService;
        Method = new LoginUnlockMethodItem(
            LoginUnlockMethod.MasterPassword,
            "Unlock with master password",
            string.Empty,
            true,
            "Master password",
            "Unlock with master password",
            UnlockCommandCommand);
    }

    public LoginPageViewModel ParentViewModel { get; }
    public LoginUnlockMethodItem Method { get; }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task UnlockCommandAsync() => TryUnlockAsync();

    internal Task TryUnlockAsync(CancellationToken cancellationToken = default)
    {
        ParentViewModel.ClearStatus();

        if (!Method.IsAvailable)
        {
            ParentViewModel.ShowError("This session cannot be unlocked with the master password.");
            return Task.CompletedTask;
        }

        if (!Method.TryValidateForSubmit())
        {
            return Task.CompletedTask;
        }

        return UnlockCoreAsync(Method.SecretInput, cancellationToken);
    }

    private async Task UnlockCoreAsync(string masterPassword, CancellationToken cancellationToken)
    {
        bool wasInitialized = ParentViewModel.HasInitializedLocalUnlock;

        try
        {
            await ParentViewModel.RunBusyAsync(async () =>
            {
                VaultUnlockOutcome outcome = await _vaultService
                    .UnlockAsync(masterPassword, cancellationToken)
                    .ConfigureAwait(true);

                await ParentViewModel.HandleUnlockOutcomeAsync(outcome, recommendUnlockSetup: !wasInitialized).ConfigureAwait(true);
            }).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ParentViewModel.ShowError(ex.Message);
        }
    }

    public void SetAvailability(bool isAvailable)
        => Method.SetAvailability(isAvailable);

    public void Reset()
        => Method.Reset();
}
