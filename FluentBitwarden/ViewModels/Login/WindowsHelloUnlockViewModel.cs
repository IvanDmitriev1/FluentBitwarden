using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Abstractions.UnlockServices;
using FluentBitwarden.Models;

namespace FluentBitwarden.ViewModels;

internal partial class WindowsHelloUnlockViewModel : ObservableObject
{
    private readonly IWindowsHelloUnlockService _windowsHelloUnlockService;

    public WindowsHelloUnlockViewModel(
        LoginPageViewModel parentViewModel,
        IWindowsHelloUnlockService windowsHelloUnlockService)
    {
        ParentViewModel = parentViewModel;
        _windowsHelloUnlockService = windowsHelloUnlockService;
        Method = new LoginUnlockMethodItem(
            LoginUnlockMethod.WindowsHello,
            "Unlock with Windows Hello",
            "Use Windows Hello to unlock your saved Bitwarden session.",
            false,
            string.Empty,
            "Unlock with Windows Hello",
            UnlockCommandCommand);
    }

    public LoginPageViewModel ParentViewModel { get; }
    public LoginUnlockMethodItem Method { get; }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task UnlockCommandAsync() => TryAutoUnlockAsync();

    internal Task TryAutoUnlockAsync(CancellationToken cancellationToken = default)
    {
        ParentViewModel.ClearStatus();

        if (!Method.IsAvailable)
        {
            ParentViewModel.ShowError("Windows Hello unlock is not available.");
            return Task.CompletedTask;
        }

        return UnlockCoreAsync(cancellationToken);
    }

    private async Task UnlockCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ParentViewModel.RunBusyAsync(async () =>
            {
                await _windowsHelloUnlockService.UnlockAsync(ParentViewModel.RequireSession(), cancellationToken);
                await ParentViewModel.CompleteUnlockAsync();
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Debug.WriteLine($"{nameof(WindowsHelloUnlockViewModel)}: auto-unlock canceled.");
        }
        catch (Exception ex)
        {
            ParentViewModel.ShowError(ex);
        }
    }

    public void SetAvailability(bool isAvailable)
        => Method.SetAvailability(isAvailable);

    public void Reset()
        => Method.Reset();
}
