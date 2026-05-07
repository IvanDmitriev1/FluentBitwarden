using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Data.Abstractions;
using FluentBitwarden.Modules.AppState.Abstractions;
using FluentBitwarden.Views.Settings.Models;
using Microsoft.UI.Xaml;
using FluentBitwarden.Modules.AppState;
using FluentBitwarden.Modules.Session.Abstractions;
using FluentBitwarden.Modules.Session.Models;
using FluentBitwarden.Modules.Session.Services;

namespace FluentBitwarden.Views.Settings;

public sealed partial class SettingsPageViewModel : ObservableObject
{
    private readonly IAccountSessionManager _accountSessionManager;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly TpmCngAccountUnlockMethod _tpmCngAccountUnlockMethod;

    public SettingsPageViewModel(
        IThemeService themeService,
        IAccountSessionManager accountSessionManager,
        IUnitOfWorkFactory unitOfWorkFactory,
        TpmCngAccountUnlockMethod tpmCngAccountUnlockMethod)
    {
        _accountSessionManager = accountSessionManager;
        _unitOfWorkFactory = unitOfWorkFactory;
        _tpmCngAccountUnlockMethod = tpmCngAccountUnlockMethod;
        Theme = AppSettingKeys.Appearance.ThemeKey.Create(themeService.Apply);
    }

    public SettingValue<ElementTheme> Theme { get; }

    [RelayCommand]
    private void Test()
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var accountSession = _accountSessionManager.RequireActiveSession;
        _tpmCngAccountUnlockMethod.EnableTmpCngUnlock(accountSession);

        unitOfWork.AccountProfileRepository.SetUnlockMethods(accountSession.Profile.UserId,
            UnlockMethodType.MasterPassword | UnlockMethodType.WindowsHello);

        unitOfWork.SaveChanges();
    }

    [RelayCommand]
    private void Test2()
    {
        var accountSession = _accountSessionManager.RequireActiveSession;
        var f = _accountSessionManager.Unlock(new AccountUnlockRequest.TpmCngRequest(accountSession.Profile));
    }
}
