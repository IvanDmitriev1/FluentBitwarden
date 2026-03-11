using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Abstractions;
using FluentBitwarden.Models;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.ViewModels;

internal partial class PinUnlockViewModel : ObservableObject
{
    private readonly IVaultService _vaultService;

    public PinUnlockViewModel(
        LoginPageViewModel parentViewModel,
        IVaultService vaultService)
    {
        ParentViewModel = parentViewModel;
        _vaultService = vaultService;
        Method = new LoginUnlockMethodItem(
            LoginUnlockMethod.Pin,
            "Unlock with PIN",
            string.Empty,
            true,
            "App PIN",
            "Unlock with PIN",
            UnlockCommandCommand);
    }

    public LoginPageViewModel ParentViewModel { get; }
    public LoginUnlockMethodItem Method { get; }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task UnlockCommandAsync()
        => TryUnlockAsync();

    internal Task TryUnlockAsync(CancellationToken cancellationToken = default)
    {
        ParentViewModel.ClearStatus();

        if (!Method.IsAvailable)
        {
            ParentViewModel.ShowError("PIN unlock is not available.");
            return Task.CompletedTask;
        }

        if (!Method.TryValidateForSubmit())
        {
            return Task.CompletedTask;
        }

        string normalizedPin = Method.SecretInput.Trim();
        return UnlockCoreAsync(normalizedPin, cancellationToken);
    }

    private async Task UnlockCoreAsync(string normalizedPin, CancellationToken cancellationToken)
    {
        try
        {
            await ParentViewModel.RunBusyAsync(async () =>
            {
                VaultUnlockOutcome outcome = await _vaultService
                    .UnlockAsync(normalizedPin, cancellationToken)
                    .ConfigureAwait(true);

                await ParentViewModel.HandleUnlockOutcomeAsync(outcome).ConfigureAwait(true);
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
