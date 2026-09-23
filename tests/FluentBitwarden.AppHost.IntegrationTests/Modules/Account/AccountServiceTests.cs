using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BitwardenApi.Identity;
using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.AppHost.Modules.Account.Services;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;
using FluentBitwarden.Contracts.Infrastructure.WindowsHello;
using NSubstitute;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Account;

public sealed class AccountServiceTests(AccountRepositoryFixture fixture)
    : IClassFixture<AccountRepositoryFixture>
{
    [Fact]
    public void Account_reads_return_profiles_from_sqlite()
    {
        using var database = fixture.CreateDatabase();
        using var context = new AccountServiceTestContext(database);
        var zeta = AccountTestData.Profile(AccountTestData.FirstUserId, "zeta@example.test", "zeta");
        var alpha = AccountTestData.Profile(AccountTestData.SecondUserId, "alpha@example.test", "alpha");
        AccountRepositoryTestHelper.InsertAccount(database, zeta);
        AccountRepositoryTestHelper.InsertAccount(database, alpha);

        Assert.Equal([alpha, zeta], context.Service.GetAccounts());
        Assert.Equal(alpha, context.Service.GetAccount(alpha.UserId));
        Assert.Null(context.Service.GetAccount(UserId.Parse(AccountTestData.ThirdUserId)));
    }

    [Fact]
    public async Task AuthenticateAsync_when_identity_rejects_does_not_persist_account_material_or_token()
    {
        using var database = fixture.CreateDatabase();
        using var context = new AccountServiceTestContext(database);
        var profile = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        var rejected = new SessionTokenResult<TokenAuthenticatedModel>.Rejected(
            new SessionTokenRejection(SessionTokenRejectionKind.InvalidCredentials, "Synthetic rejection."));
        context.IdentityApi.AuthenticateWithPasswordAndTwoFactorAsync(
                Arg.Any<PasswordTwoFactorAuthenticationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SessionTokenResult<TokenAuthenticatedModel>>(rejected));

        var result = await context.Service.AuthenticateAsync(
            CreateTwoFactorRequest(profile),
            TestContext.Current.CancellationToken);

        Assert.Equal(new AccountAuthenticationOutcome.InvalidCredentials("Synthetic rejection."), result);
        Assert.Null(context.Service.GetAccount(profile.UserId));
        Assert.Null(new AccountKeyMaterialRepository(context.UnitOfWork).GetById(profile.UserId));
        Assert.Equal(SessionRefreshToken.Empty, new AccountBitwardenSessionTokenRepository(context.UnitOfWork).Get(profile.UserId));
    }

    [Fact]
    public async Task AuthenticateAsync_when_identity_authenticates_persists_profile_key_material_and_refresh_token()
    {
        using var database = fixture.CreateDatabase();
        using var context = new AccountServiceTestContext(database);
        const string email = "authenticated@example.test";
        var profile = AccountTestData.Profile(AccountTestData.FirstUserId, email, "first");
        var keyMaterial = AccountTestData.KeyMaterial(
            AccountTestData.FirstUserId,
            new KdfConfig.Pbkdf2(600000),
            "authenticated");
        SessionRefreshToken refreshToken = SessionRefreshToken.Parse("synthetic-auth-refresh-token");
        TokenAuthenticatedModel model = CreateAuthenticatedModel(profile.UserId, email, keyMaterial, refreshToken);
        context.IdentityApi.AuthenticateWithPasswordAndTwoFactorAsync(
                Arg.Any<PasswordTwoFactorAuthenticationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SessionTokenResult<TokenAuthenticatedModel>>(
                new SessionTokenResult<TokenAuthenticatedModel>.Success(model)));

        AccountAuthenticationOutcome result = await context.Service.AuthenticateAsync(
            CreateTwoFactorRequest(profile),
            TestContext.Current.CancellationToken);

        using var persistedReadUnitOfWork = database.CreateUnitOfWork();
        Assert.Equal(new AccountAuthenticationOutcome.Success(profile), result);
        Assert.Equal(profile, new AccountProfileRepository(persistedReadUnitOfWork).GetById(profile.UserId));
        Assert.Equal(keyMaterial, new AccountKeyMaterialRepository(persistedReadUnitOfWork).GetById(profile.UserId));
        Assert.Equal(refreshToken, new AccountBitwardenSessionTokenRepository(persistedReadUnitOfWork).Get(profile.UserId));
    }

    [Fact]
    public async Task RefreshSessionTokens_rotates_the_stored_token_and_returns_the_new_session_tokens()
    {
        using var database = fixture.CreateDatabase();
        using var context = new AccountServiceTestContext(database);
        var profile = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        SessionRefreshToken oldRefreshToken = SessionRefreshToken.Parse("synthetic-old-refresh-token");
        SessionRefreshToken newRefreshToken = SessionRefreshToken.Parse("synthetic-new-refresh-token");
        SessionAccessToken accessToken = SessionAccessToken.Parse("synthetic-access-token");
        DateTimeOffset expiresAt = new(2035, 1, 2, 3, 4, 5, TimeSpan.Zero);
        AccountRepositoryTestHelper.InsertAccount(database, profile);
        AccountRepositoryTestHelper.StoreToken(database, profile.UserId, oldRefreshToken);
        context.IdentityApi.RefreshAuthenticationAsync(Arg.Any<RefreshAuthenticationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SessionTokenResult<TokenRefreshSessionModel>>(
                new SessionTokenResult<TokenRefreshSessionModel>.Success(
                    new TokenRefreshSessionModel(accessToken, newRefreshToken, default, expiresAt))));

        AccountSessionTokens result = await context.Service.RefreshSessionTokens(
            profile.BitwardenAccountContext,
            TestContext.Current.CancellationToken);

        Assert.Equal(profile.UserId, result.UserId);
        Assert.Equal(newRefreshToken, result.RefreshToken);
        Assert.Equal(accessToken, result.AccessToken);
        Assert.Equal(expiresAt, result.ExpiresAt);
        Assert.Equal(profile.Environment, result.ClientContext.Environment);
        using var persistedReadUnitOfWork = database.CreateUnitOfWork();
        Assert.Equal(newRefreshToken, new AccountBitwardenSessionTokenRepository(persistedReadUnitOfWork).Get(profile.UserId));
        _ = context.IdentityApi.Received(1).RefreshAuthenticationAsync(
            Arg.Is<RefreshAuthenticationRequest>(request =>
                request.SessionRefreshToken == oldRefreshToken && request.Context.Environment == profile.Environment),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshSessionTokens_when_identity_rejects_preserves_the_stored_refresh_token()
    {
        using var database = fixture.CreateDatabase();
        using var context = new AccountServiceTestContext(database);
        var profile = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        SessionRefreshToken oldRefreshToken = SessionRefreshToken.Parse("synthetic-existing-refresh-token");
        var rejected = new SessionTokenResult<TokenRefreshSessionModel>.Rejected(
            new SessionTokenRejection(SessionTokenRejectionKind.InvalidCredentials, "Synthetic rejection."));
        AccountRepositoryTestHelper.InsertAccount(database, profile);
        AccountRepositoryTestHelper.StoreToken(database, profile.UserId, oldRefreshToken);
        context.IdentityApi.RefreshAuthenticationAsync(Arg.Any<RefreshAuthenticationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SessionTokenResult<TokenRefreshSessionModel>>(rejected));

        AccountSessionRefreshException exception = await Assert.ThrowsAsync<AccountSessionRefreshException>(
            () => context.Service.RefreshSessionTokens(
                profile.BitwardenAccountContext,
                TestContext.Current.CancellationToken));

        using var persistedReadUnitOfWork = database.CreateUnitOfWork();
        Assert.Equal(rejected, exception.Outcome);
        Assert.Equal(oldRefreshToken, new AccountBitwardenSessionTokenRepository(persistedReadUnitOfWork).Get(profile.UserId));
    }

    [Fact]
    public void UnlockKey_without_stored_material_requires_online_reauthentication()
    {
        using var database = fixture.CreateDatabase();
        using var context = new AccountServiceTestContext(database);
        UserId userId = UserId.Parse(AccountTestData.FirstUserId);

        AccountKeyUnlockResult result = context.Service.UnlockKey(
            userId,
            new AccountUnlockMethod.MasterPassword("synthetic-password"));

        Assert.IsType<AccountKeyUnlockResult.RequiresOnlineReauthentication>(result);
    }

    [Fact]
    public void UnlockKey_with_correct_master_password_returns_the_decrypted_user_key()
    {
        using var database = fixture.CreateDatabase();
        using var context = new AccountServiceTestContext(database);
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        const string password = "synthetic-master-password";
        byte[] expectedUserKey = [0x11, 0x22, 0x33, 0x44, 0x55];
        var keyMaterial = CreateMasterPasswordUnlockMaterial(account.UserId, password, expectedUserKey);
        AccountRepositoryTestHelper.InsertAccount(database, account);
        AccountRepositoryTestHelper.UpsertKeyMaterial(database, keyMaterial);

        AccountKeyUnlockResult result = context.Service.UnlockKey(
            account.UserId,
            new AccountUnlockMethod.MasterPassword(password));

        var success = Assert.IsType<AccountKeyUnlockResult.Success>(result);
        using (success.UserKey)
        {
            Assert.Equal(account.UserId, success.UserKey.UserId);
            Assert.Equal(expectedUserKey, success.UserKey.Key.ToArray());
        }
    }

    [Fact]
    public void UnlockKey_with_wrong_master_password_returns_failure()
    {
        using var database = fixture.CreateDatabase();
        using var context = new AccountServiceTestContext(database);
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        const string password = "synthetic-master-password";
        var keyMaterial = CreateMasterPasswordUnlockMaterial(
            account.UserId,
            password,
            [0x11, 0x22, 0x33, 0x44, 0x55]);
        AccountRepositoryTestHelper.InsertAccount(database, account);
        AccountRepositoryTestHelper.UpsertKeyMaterial(database, keyMaterial);

        AccountKeyUnlockResult result = context.Service.UnlockKey(
            account.UserId,
            new AccountUnlockMethod.MasterPassword("synthetic-wrong-password"));

        try
        {
            Assert.IsType<AccountKeyUnlockResult.Failure>(result);
        }
        finally
        {
            if (result is AccountKeyUnlockResult.Success unexpectedSuccess)
                unexpectedSuccess.UserKey.Dispose();
        }
    }

    [Fact]
    public void UnlockKey_with_Windows_Hello_forwards_stored_material_and_owner_window()
    {
        using var database = fixture.CreateDatabase();
        using var context = new AccountServiceTestContext(database);
        var keyMaterial = AccountTestData.KeyMaterial(
            AccountTestData.FirstUserId,
            new KdfConfig.Pbkdf2(600000),
            "hello");
        AccountRepositoryTestHelper.InsertAccount(
            database,
            AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first"));
        NativeWindowHandle ownerWindow = new(42);
        using var unlockedUserKey = new UnlockedUserKey(keyMaterial.UserId, [0x10, 0x20, 0x30]);
        var expected = new AccountKeyUnlockResult.Success(unlockedUserKey);
        AccountRepositoryTestHelper.UpsertKeyMaterial(database, keyMaterial);
        context.WindowsHelloService.Unlock(keyMaterial, (IntPtr)ownerWindow.Value).Returns(expected);

        AccountKeyUnlockResult result = context.Service.UnlockKey(
            keyMaterial.UserId,
            new AccountUnlockMethod.WindowsHello(ownerWindow));

        Assert.Equal(expected, result);
        context.WindowsHelloService.Received(1).Unlock(keyMaterial, (IntPtr)ownerWindow.Value);
    }

    private AccountAuthenticationRequest.TwoFactor CreateTwoFactorRequest(AccountProfile profile) => new(
        new BitwardenClientContext(
            profile.Environment,
            new DeviceInfo(
                DeviceIdentifier.Parse("synthetic-device-id"),
                DeviceName.Parse("synthetic-device-name"))),
        profile.Email,
        "synthetic-server-authorization-hash",
        new IdentityTwoFactorProof("123456", IdentityTwoFactorProviderType.Authenticator));

    private TokenAuthenticatedModel CreateAuthenticatedModel(
        UserId userId,
        string email,
        AccountKeyMaterial keyMaterial,
        SessionRefreshToken refreshToken)
    {
        var jwt = new JwtSecurityToken(claims:
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email)
        ]);
        string accessTokenValue = new JwtSecurityTokenHandler().WriteToken(jwt);

        return new TokenAuthenticatedModel(
            SessionAccessToken.Parse(accessTokenValue),
            refreshToken,
            default,
            new DateTimeOffset(2035, 1, 2, 3, 4, 5, TimeSpan.Zero),
            keyMaterial.ProtectedPrivateKey,
            new MasterPasswordUnlockModel(
                keyMaterial.KdfConfig,
                keyMaterial.Salt,
                keyMaterial.ProtectedUserKey));
    }

    private AccountKeyMaterial CreateMasterPasswordUnlockMaterial(
        UserId userId,
        string masterPassword,
        byte[] expectedUserKey)
    {
        const string salt = "synthetic-master-password-salt";
        var kdfConfig = new KdfConfig.Pbkdf2(2);
        using MasterKey masterKey = MasterKey.Derive(masterPassword, salt, kdfConfig);
        using StretchedMasterKey stretchedMasterKey = masterKey.Stretch();
        var protectedUserKey = ProtectedUserKey.Create(
            EncString.Encrypt(expectedUserKey, stretchedMasterKey.Span));
        AccountKeyMaterial keyMaterial = AccountTestData.KeyMaterial(
            userId.ToString(),
            kdfConfig,
            "master-password-unlock");

        return keyMaterial with
        {
            Salt = salt,
            ProtectedUserKey = protectedUserKey
        };
    }

    private sealed class AccountServiceTestContext : IDisposable
    {
        public AccountServiceTestContext(AccountRepositoryTestDatabase database)
        {
            UnitOfWork = database.CreateUnitOfWork();
            IdentityApi = Substitute.For<IIdentityApi>();
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
}
