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
    private readonly WindowsHelloAccountUnlockMethod _windowsHelloAccountUnlockMethod;

    public SettingsPageViewModel(
        IThemeService themeService,
        IAccountSessionManager accountSessionManager,
        IUnitOfWorkFactory unitOfWorkFactory,
        WindowsHelloAccountUnlockMethod windowsHelloAccountUnlockMethod)
    {
        _accountSessionManager = accountSessionManager;
        _unitOfWorkFactory = unitOfWorkFactory;
        _windowsHelloAccountUnlockMethod = windowsHelloAccountUnlockMethod;
        Theme = AppSettingKeys.Appearance.ThemeKey.Create(themeService.Apply);
    }

    public SettingValue<ElementTheme> Theme { get; }

    [RelayCommand]
    private void Test()
    {
        using var unitOfWork = _unitOfWorkFactory.Create();

        var accountSession = _accountSessionManager.RequireActiveSession;
        _windowsHelloAccountUnlockMethod.EnableWindowsHelloUnlock(accountSession);

        unitOfWork.AccountProfileRepository.SetUnlockMethods(accountSession.Profile.UserId,
            UnlockMethodType.MasterPassword | UnlockMethodType.WindowsHello);

        unitOfWork.SaveChanges();
    }

    [RelayCommand]
    private void Test2()
    {
        var accountSession = _accountSessionManager.RequireActiveSession;
        var f = _accountSessionManager.Unlock(new AccountUnlockRequest.WindowsHelloRequest(accountSession.Profile));
    }
}
