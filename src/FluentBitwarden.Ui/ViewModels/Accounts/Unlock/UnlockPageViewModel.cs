using CommunityToolkit.Mvvm.Input;
using System.Diagnostics.CodeAnalysis;
using FluentBitwarden.Application.Abstractions;
using FluentBitwarden.Application.Models;
using FluentBitwarden.Contracts.AppSession.Unlock;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Infrastructure.Window;
using FluentBitwarden.Views.Accounts;
using FluentBitwarden.Views.Shell;
using FluentBitwarden.Views.Startup;

namespace FluentBitwarden.ViewModels.Accounts.Unlock;

public sealed partial class UnlockPageViewModel(
    INavigation navigation,
    IAppCoordinator appCoordinator,
    IWindowManager windowManager) : ObservableObject, INavigationAware<UnlockPageParameter>
{
    [ObservableProperty]
    public partial AccountProfile? SelectedAccount { get; private set; }

    [MemberNotNull(nameof(SelectedAccount))]
    public ValueTask OnNavigatedToAsync(
        UnlockPageParameter param,
        CancellationToken cancellationToken)
    {
        SelectedAccount = param.FavoriteAccountProfile;
        appCoordinator.SessionStateApplied += OnSessionStateApplied;

        return ValueTask.CompletedTask;
    }

    public ValueTask OnNavigatedFromAsync(CancellationToken cancellationToken)
    {
        appCoordinator.SessionStateApplied -= OnSessionStateApplied;
        return ValueTask.CompletedTask;
    }

    private void OnSessionStateApplied(
        AppSessionState state,
        UnlockPageParameter? unlockParameter,
        OpenVaultCipherIntent? openIntent)
    {
        switch (state)
        {
            case AppSessionState.LoggedOut:
                navigation.Root.Navigate<LogInFlowPage>(NavigationKind.Reset);
                break;
            case AppSessionState.Locked when unlockParameter is not null:
                navigation.Root.Navigate<UnlockPage, UnlockPageParameter>(
                    unlockParameter,
                    NavigationKind.Reset);
                break;
            case AppSessionState.Unlocked when windowManager.ActiveMode == WindowMode.Main:
                if (openIntent is null)
                {
                    navigation.Root.Navigate<ShellPage>(NavigationKind.Reset);
                }
                else
                {
                    navigation.Root.Navigate<ShellPage, OpenVaultCipherIntent>(
                        openIntent,
                        NavigationKind.Reset);
                }

                break;
            case AppSessionState.Unlocked:
                navigation.Root.Navigate<LoadingPage>(NavigationKind.Reset);
                break;
        }
    }

    [RelayCommand]
    private void VaultUnlockResult(SessionUnlockOutcome result)
    {
        ArgumentNullException.ThrowIfNull(SelectedAccount);

        switch (result)
        {
            case SessionUnlockOutcome.Failure:
                //TODO
                break;
            case SessionUnlockOutcome.RequiresOnlineReauth:
                //appCoordinator.RequireSignIn(SelectedAccount);
                //TODO
                break;
        }
    }
}
