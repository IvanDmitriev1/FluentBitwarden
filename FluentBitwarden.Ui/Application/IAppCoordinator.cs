using FluentBitwarden.Contracts.Modules.Accounts.StoredAccount;

namespace FluentBitwarden.Application;

public interface IAppCoordinator
{
    AppSessionState SessionState { get; }

    void RefreshSession();
    void CompleteSignIn(AccountProfile account);
    void RequireSignIn(AccountProfile account);

    void BeginAddAccount();
    void SwitchAccount(UserId accountId);
    void LogOut(UserId accountId);
}
