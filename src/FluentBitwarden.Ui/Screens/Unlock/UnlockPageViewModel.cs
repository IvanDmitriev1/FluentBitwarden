using CommunityToolkit.Mvvm.Input;
using FluentBitwarden.Contracts.AppSession;
using FluentBitwarden.Contracts.AppSession.Unlock;
using FluentBitwarden.Contracts.Infrastructure.WindowsHello;
using FluentBitwarden.Contracts.Modules.Accounts;
using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;
using FluentBitwarden.Ui.Windows.Abstractions;
using Navis.WinUI.Abstractions;

namespace FluentBitwarden.Ui.Screens.Unlock;

public sealed partial class UnlockPageViewModel(
    IAppSessionClient appSessionClient,
    IAccountWindowsHelloIntegrationClient accountWindowsHelloIntegrationClient,
    IWindowManager windowManager,
    ILogger<UnlockPageViewModel> logger) : ObservableObject, INavigationAware<AccountProfile>
{
    private CancellationTokenSource? _pageCancellation;
    private int _statusRequestId;

    [ObservableProperty]
    public partial AccountProfile? SelectedAccount { get; private set; }

    [ObservableProperty]
    public partial string? Error { get; private set; }

    [ObservableProperty]
    public partial bool IsUnlocking { get; private set; }

    [ObservableProperty]
    public partial int PasswordFocusRequest { get; private set; }

    [ObservableProperty]
    public partial bool IsWindowHelloButtonVisible { get; private set; }

    public async ValueTask OnNavigatedToAsync(AccountProfile profile, CancellationToken cancellationToken)
    {
        _pageCancellation?.Cancel();
        _pageCancellation?.Dispose();
        _pageCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        SelectedAccount = profile;
        Error = null;
        int requestId = ++_statusRequestId;

        await RefreshWindowsHelloStatusAsync(profile, requestId, _pageCancellation.Token);
    }

    public ValueTask OnNavigatedFromAsync(CancellationToken cancellationToken)
    {
        ++_statusRequestId;
        _pageCancellation?.Cancel();
        _pageCancellation?.Dispose();
        _pageCancellation = null;
        IsUnlocking = false;
        Error = null;

        return ValueTask.CompletedTask;
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task Unlock(string password)
    {
        if (SelectedAccount is not { } account)
            return Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(password))
        {
            PasswordFocusRequest++;
            return Task.CompletedTask;
        }

        return OnUnlockCore(new SessionUnlockRequest.MasterPasswordRequest(account.UserId, password));
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private Task UnlockWithWindowsHello()
    {
        if (SelectedAccount is not { } account)
            return Task.CompletedTask;

        return OnUnlockCore(new SessionUnlockRequest.WindowsHelloRequest(
            account.UserId,
            windowManager.NativeWindowHandle));
    }

    private async Task OnUnlockCore(SessionUnlockRequest request)
    {
        CancellationToken cancellationToken = _pageCancellation?.Token ?? CancellationToken.None;
        IsUnlocking = true;

        try
        {
            SessionUnlockOutcome result = await appSessionClient.UnlockAsync(request, cancellationToken);

            if (result is SessionUnlockOutcome.Failure failure)
            {
                Error = failure.Reason;
                PasswordFocusRequest++;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            IsUnlocking = false;
        }
    }

    private async Task RefreshWindowsHelloStatusAsync(
        AccountProfile account,
        int requestId,
        CancellationToken cancellationToken)
    {
        try
        {
            WindowsHelloEnrollmentStatus status = await accountWindowsHelloIntegrationClient.GetEnrollmentAsync(
                new GetWindowsHelloEnrollmentRequest(account.UserId), cancellationToken);

            if (!cancellationToken.IsCancellationRequested && requestId == _statusRequestId)
                IsWindowHelloButtonVisible = status == WindowsHelloEnrollmentStatus.Enrolled;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception e)
        {
            logger.LogError(e, "Windows Hello status could not be refreshed.");
        }
    }
}
