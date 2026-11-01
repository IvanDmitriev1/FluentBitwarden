using BitwardenApi.Identity;
using BitwardenApi.Cryptography.KeyDerivation;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Account.Services;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Account;

internal sealed class AccountServiceTestContext : IDisposable
{
    public AccountServiceTestContext(AccountRepositoryTestDatabase database)
    {
        UnitOfWork = database.CreateUnitOfWork();
        IdentityApi = Substitute.For<IIdentityApi>();
        IdentityApi.GetPasswordPreloginAsync(
                Arg.Any<BitwardenClientContext>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PasswordPreloginData(new KdfConfig.Pbkdf2(2), null)));
        WebAuthnIdentityApi = Substitute.For<IWebAuthnIdentityApi>();
        WindowsHelloService = Substitute.For<IAccountWindowsHelloService>();
        Service = new AccountService(
            UnitOfWork,
            WindowsHelloService,
            IdentityApi,
            new AccountAuthenticatorService(IdentityApi, WebAuthnIdentityApi),
            new AccountKeyMaterialRepository(UnitOfWork),
            new AccountBitwardenSessionTokenRepository(UnitOfWork),
            new AccountProfileRepository(UnitOfWork));
    }

    public UnitOfWork UnitOfWork { get; }
    public IIdentityApi IdentityApi { get; }
    public IWebAuthnIdentityApi WebAuthnIdentityApi { get; }
    public IAccountWindowsHelloService WindowsHelloService { get; }
    public AccountService Service { get; }

    public void Dispose() => UnitOfWork.Dispose();
}
