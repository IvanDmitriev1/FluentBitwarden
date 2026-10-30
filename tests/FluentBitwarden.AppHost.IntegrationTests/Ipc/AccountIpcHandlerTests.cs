using FluentBitwarden.AppHost.AppSession.Contracts;
using FluentBitwarden.AppHost.Ipc;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;

namespace FluentBitwarden.AppHost.IntegrationTests.Ipc;

public sealed class AccountIpcHandlerTests
{
    [Fact]
    public async Task Authenticate_after_success_publishes_account_to_session_operations()
    {
        AccountProfile account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        var accountService = Substitute.For<IAccountService>();
        accountService.AuthenticateAsync(Arg.Any<AccountAuthenticationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AccountAuthenticationOutcome>(new AccountAuthenticationOutcome.Success(account)));
        var sessionOperations = Substitute.For<IAppSessionOperations>();
        var handler = new AccountIpcHandler(accountService, sessionOperations);

        AccountAuthenticationOutcome result = await handler.AuthenticateAsync(null!, TestContext.Current.CancellationToken);

        Assert.Equal(new AccountAuthenticationOutcome.Success(account), result);
        await sessionOperations.Received(1).AuthenticateAsync(account, TestContext.Current.CancellationToken);
    }
}
