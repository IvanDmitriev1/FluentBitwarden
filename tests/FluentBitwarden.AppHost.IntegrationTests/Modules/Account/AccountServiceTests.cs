using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using FluentBitwarden.AppHost.IntegrationTests.Infrastructure;
using FluentBitwarden.AppHost.Modules.Account.Contracts;
using FluentBitwarden.Contracts.Modules.Accounts.Authentication;
using FluentBitwarden.Contracts.Infrastructure.WindowsHello;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Account;

public sealed class AccountServiceTests
{
    [Fact]
    public void Account_reads_return_profiles_from_sqlite()
    {
        using var database = new AccountRepositoryTestDatabase();
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
        using var database = new AccountRepositoryTestDatabase();
        using var context = new AccountServiceTestContext(database);
        var profile = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        var rejected = new SessionTokenResult<AuthenticatedSession>.Rejected(
            new SessionTokenRejection(SessionTokenRejectionKind.InvalidCredentials, "Synthetic rejection."));
        context.IdentityApi.AuthenticateWithPasswordAndTwoFactorAsync(
                Arg.Any<PasswordTwoFactorAuthenticationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SessionTokenResult<AuthenticatedSession>>(rejected));

        var result = await context.Service.AuthenticateAsync(
            CreateTwoFactorRequest(profile),
            TestContext.Current.CancellationToken);

        Assert.Equal(new AccountAuthenticationOutcome.InvalidCredentials("Synthetic rejection."), result);
        Assert.Null(context.Service.GetAccount(profile.UserId));
        Assert.Null(new AccountKeyMaterialRepository(context.UnitOfWork).GetById(profile.UserId));
        Assert.Equal(SessionRefreshToken.Empty, new AccountBitwardenSessionTokenRepository(context.UnitOfWork).Get(profile.UserId));
    }

    [Fact]
    public async Task AuthenticateAsync_when_identity_requires_device_verification_returns_that_outcome_without_persisting()
    {
        using var database = new AccountRepositoryTestDatabase();
        using var context = new AccountServiceTestContext(database);
        var profile = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        var rejected = new SessionTokenResult<AuthenticatedSession>.Rejected(
            new SessionTokenRejection(
                SessionTokenRejectionKind.DeviceVerificationRequired,
                "Synthetic device verification required."));
        context.IdentityApi.AuthenticateWithPasswordAndTwoFactorAsync(
                Arg.Any<PasswordTwoFactorAuthenticationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SessionTokenResult<AuthenticatedSession>>(rejected));

        AccountAuthenticationOutcome result = await context.Service.AuthenticateAsync(
            CreateTwoFactorRequest(profile),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            new AccountAuthenticationOutcome.DeviceVerificationRequired("Synthetic device verification required."),
            result);
        Assert.Null(context.Service.GetAccount(profile.UserId));
        Assert.Null(new AccountKeyMaterialRepository(context.UnitOfWork).GetById(profile.UserId));
        Assert.Equal(SessionRefreshToken.Empty, new AccountBitwardenSessionTokenRepository(context.UnitOfWork).Get(profile.UserId));
    }

    [Fact]
    public async Task AuthenticateAsync_when_password_login_requires_two_factor_preserves_the_challenge_and_password_context()
    {
        using var database = new AccountRepositoryTestDatabase();
        using var context = new AccountServiceTestContext(database);
        var profile = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        var request = new AccountAuthenticationRequest.Password(
            new BitwardenClientContext(
                profile.Environment,
                new DeviceInfo(
                    DeviceIdentifier.Parse("synthetic-device-id"),
                    DeviceName.Parse("synthetic-device-name"))),
            profile.Email,
            "synthetic-master-password");
        var challenge = new IdentityTwoFactorChallenge(
        (IdentityTwoFactorProviderOption[])[
            new IdentityTwoFactorProviderOption(IdentityTwoFactorProviderType.Email),
            new IdentityTwoFactorProviderOption(IdentityTwoFactorProviderType.Authenticator)
        ]);
        var rejected = new SessionTokenResult<AuthenticatedSession>.Rejected(
            new SessionTokenRejection(
                SessionTokenRejectionKind.TwoFactorRequired,
                "Synthetic two-factor challenge.",
                challenge));
        PasswordAuthenticationRequest? identityRequest = null;
        context.IdentityApi.AuthenticateWithPasswordAsync(
                Arg.Do<PasswordAuthenticationRequest>(value => identityRequest = value),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SessionTokenResult<AuthenticatedSession>>(rejected));

        AccountAuthenticationOutcome result = await context.Service.AuthenticateAsync(
            request,
            TestContext.Current.CancellationToken);

        var twoFactor = Assert.IsType<AccountAuthenticationOutcome.TwoFactorRequired>(result);
        Assert.Equal(challenge, twoFactor.Challenge);
        Assert.Equal(request.Email, twoFactor.Email);
        Assert.NotNull(identityRequest);
        Assert.Equal(request.Context, identityRequest.Context);
        Assert.Equal(request.Email, identityRequest.Email);
        Assert.False(string.IsNullOrWhiteSpace(identityRequest.MasterPasswordHash));
        Assert.Equal(identityRequest.MasterPasswordHash, twoFactor.ServerAuthorizationHash);
        Assert.Null(context.Service.GetAccount(profile.UserId));
        Assert.Null(new AccountKeyMaterialRepository(context.UnitOfWork).GetById(profile.UserId));
        Assert.Equal(SessionRefreshToken.Empty, new AccountBitwardenSessionTokenRepository(context.UnitOfWork).Get(profile.UserId));
    }

    [Fact]
    public async Task AuthenticateAsync_when_identity_requires_two_factor_without_a_challenge_does_not_persist()
    {
        using var database = new AccountRepositoryTestDatabase();
        using var context = new AccountServiceTestContext(database);
        var profile = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        var rejected = new SessionTokenResult<AuthenticatedSession>.Rejected(
            new SessionTokenRejection(SessionTokenRejectionKind.TwoFactorRequired, "Synthetic missing challenge."));
        context.IdentityApi.AuthenticateWithPasswordAndTwoFactorAsync(
                Arg.Any<PasswordTwoFactorAuthenticationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SessionTokenResult<AuthenticatedSession>>(rejected));

        await Assert.ThrowsAsync<InvalidDataException>(() => context.Service.AuthenticateAsync(
            CreateTwoFactorRequest(profile),
            TestContext.Current.CancellationToken));

        Assert.Null(context.Service.GetAccount(profile.UserId));
        Assert.Null(new AccountKeyMaterialRepository(context.UnitOfWork).GetById(profile.UserId));
        Assert.Equal(SessionRefreshToken.Empty, new AccountBitwardenSessionTokenRepository(context.UnitOfWork).Get(profile.UserId));
    }

    [Fact]
    public async Task AuthenticateAsync_when_success_token_is_missing_the_subject_claim_does_not_persist()
    {
        using var database = new AccountRepositoryTestDatabase();
        using var context = new AccountServiceTestContext(database);
        var profile = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        var keyMaterial = AccountTestData.KeyMaterial(profile.UserId.ToString(), new KdfConfig.Pbkdf2(600000), "missing-subject");
        var validModel = CreateAuthenticatedModel(
            profile.UserId,
            profile.Email,
            keyMaterial,
            SessionRefreshToken.Parse("synthetic-auth-refresh-token"));
        var tokenWithoutSubject = new JwtSecurityToken(claims:
        (Claim[])[
            new Claim(JwtRegisteredClaimNames.Email, profile.Email)
        ]);
        var malformedModel = validModel with
        {
            SessionAccessToken = SessionAccessToken.Parse(new JwtSecurityTokenHandler().WriteToken(tokenWithoutSubject))
        };
        context.IdentityApi.AuthenticateWithPasswordAndTwoFactorAsync(
                Arg.Any<PasswordTwoFactorAuthenticationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SessionTokenResult<AuthenticatedSession>>(
                new SessionTokenResult<AuthenticatedSession>.Success(malformedModel)));

        await Assert.ThrowsAsync<InvalidDataException>(() => context.Service.AuthenticateAsync(
            CreateTwoFactorRequest(profile),
            TestContext.Current.CancellationToken));

        Assert.Null(context.Service.GetAccount(profile.UserId));
        Assert.Null(new AccountKeyMaterialRepository(context.UnitOfWork).GetById(profile.UserId));
        Assert.Equal(SessionRefreshToken.Empty, new AccountBitwardenSessionTokenRepository(context.UnitOfWork).Get(profile.UserId));
    }

    [Fact]
    public async Task AuthenticateAsync_when_success_token_is_missing_the_email_claim_does_not_persist()
    {
        using var database = new AccountRepositoryTestDatabase();
        using var context = new AccountServiceTestContext(database);
        var profile = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        var keyMaterial = AccountTestData.KeyMaterial(profile.UserId.ToString(), new KdfConfig.Pbkdf2(600000), "missing-email");
        var validModel = CreateAuthenticatedModel(
            profile.UserId,
            profile.Email,
            keyMaterial,
            SessionRefreshToken.Parse("synthetic-auth-refresh-token"));
        var tokenWithoutEmail = new JwtSecurityToken(claims:
        (Claim[])[
            new Claim(JwtRegisteredClaimNames.Sub, profile.UserId.ToString())
        ]);
        var malformedModel = validModel with
        {
            SessionAccessToken = SessionAccessToken.Parse(new JwtSecurityTokenHandler().WriteToken(tokenWithoutEmail))
        };
        context.IdentityApi.AuthenticateWithPasswordAndTwoFactorAsync(
                Arg.Any<PasswordTwoFactorAuthenticationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SessionTokenResult<AuthenticatedSession>>(
                new SessionTokenResult<AuthenticatedSession>.Success(malformedModel)));

        await Assert.ThrowsAsync<InvalidDataException>(() => context.Service.AuthenticateAsync(
            CreateTwoFactorRequest(profile),
            TestContext.Current.CancellationToken));

        Assert.Null(context.Service.GetAccount(profile.UserId));
        Assert.Null(new AccountKeyMaterialRepository(context.UnitOfWork).GetById(profile.UserId));
        Assert.Equal(SessionRefreshToken.Empty, new AccountBitwardenSessionTokenRepository(context.UnitOfWork).Get(profile.UserId));
    }

    [Fact]
    public async Task AuthenticateAsync_when_identity_authenticates_persists_profile_key_material_and_refresh_token()
    {
        using var database = new AccountRepositoryTestDatabase();
        using var context = new AccountServiceTestContext(database);
        const string email = "authenticated@example.test";
        var profile = AccountTestData.Profile(AccountTestData.FirstUserId, email, "first");
        var keyMaterial = AccountTestData.KeyMaterial(
            AccountTestData.FirstUserId,
            new KdfConfig.Pbkdf2(600000),
            "authenticated");
        SessionRefreshToken refreshToken = SessionRefreshToken.Parse("synthetic-auth-refresh-token");
        AuthenticatedSession model = CreateAuthenticatedModel(profile.UserId, email, keyMaterial, refreshToken);
        context.IdentityApi.AuthenticateWithPasswordAndTwoFactorAsync(
                Arg.Any<PasswordTwoFactorAuthenticationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SessionTokenResult<AuthenticatedSession>>(
                new SessionTokenResult<AuthenticatedSession>.Success(model)));

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
        using var database = new AccountRepositoryTestDatabase();
        using var context = new AccountServiceTestContext(database);
        var profile = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        SessionRefreshToken oldRefreshToken = SessionRefreshToken.Parse("synthetic-old-refresh-token");
        SessionRefreshToken newRefreshToken = SessionRefreshToken.Parse("synthetic-new-refresh-token");
        SessionAccessToken accessToken = SessionAccessToken.Parse("synthetic-access-token");
        DateTimeOffset expiresAt = new(2035, 1, 2, 3, 4, 5, TimeSpan.Zero);
        AccountRepositoryTestHelper.InsertAccount(database, profile);
        AccountRepositoryTestHelper.StoreToken(database, profile.UserId, oldRefreshToken);
        context.IdentityApi.RefreshAuthenticationAsync(Arg.Any<RefreshAuthenticationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SessionTokenResult<RefreshedSession>>(
                new SessionTokenResult<RefreshedSession>.Success(
                    new RefreshedSession(accessToken, newRefreshToken, default, expiresAt))));

        AccountSessionTokens result = await context.Service.RefreshSessionTokensAsync(profile.BitwardenAccountContext);

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
        using var database = new AccountRepositoryTestDatabase();
        using var context = new AccountServiceTestContext(database);
        var profile = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        SessionRefreshToken oldRefreshToken = SessionRefreshToken.Parse("synthetic-existing-refresh-token");
        var rejected = new SessionTokenResult<RefreshedSession>.Rejected(
            new SessionTokenRejection(SessionTokenRejectionKind.InvalidCredentials, "Synthetic rejection."));
        AccountRepositoryTestHelper.InsertAccount(database, profile);
        AccountRepositoryTestHelper.StoreToken(database, profile.UserId, oldRefreshToken);
        context.IdentityApi.RefreshAuthenticationAsync(Arg.Any<RefreshAuthenticationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SessionTokenResult<RefreshedSession>>(rejected));

        AccountSessionRefreshException exception = await Assert.ThrowsAsync<AccountSessionRefreshException>(
            () => context.Service.RefreshSessionTokensAsync(profile.BitwardenAccountContext));

        using var persistedReadUnitOfWork = database.CreateUnitOfWork();
        Assert.Equal(rejected, exception.Outcome);
        Assert.Equal(oldRefreshToken, new AccountBitwardenSessionTokenRepository(persistedReadUnitOfWork).Get(profile.UserId));
    }

    [Fact]
    public void UnlockKey_without_stored_material_requires_online_reauthentication()
    {
        using var database = new AccountRepositoryTestDatabase();
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
        using var database = new AccountRepositoryTestDatabase();
        using var context = new AccountServiceTestContext(database);
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        const string password = "synthetic-master-password";
        byte[] expectedUserKey = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        var keyMaterial = CreateMasterPasswordUnlockMaterial(account.UserId, password, expectedUserKey);
        AccountRepositoryTestHelper.InsertAccount(database, account);
        AccountRepositoryTestHelper.UpsertKeyMaterial(database, keyMaterial);

        AccountKeyUnlockResult result = context.Service.UnlockKey(
            account.UserId,
            new AccountUnlockMethod.MasterPassword(password));

        var success = Assert.IsType<AccountKeyUnlockResult.Success>(result);
        using (success.AccountKeySession)
        {
            Assert.Equal(account.UserId, success.AccountKeySession.UserId);
            Assert.Equal(expectedUserKey, success.AccountKeySession.UserKey.Key.ToArray());
        }

        Assert.Throws<ObjectDisposedException>(() => success.AccountKeySession.UserKey.Key.ToArray());
    }

    [Fact]
    public void UnlockKey_with_wrong_master_password_returns_failure()
    {
        using var database = new AccountRepositoryTestDatabase();
        using var context = new AccountServiceTestContext(database);
        var account = AccountTestData.Profile(AccountTestData.FirstUserId, "user@example.test", "first");
        const string password = "synthetic-master-password";
        var keyMaterial = CreateMasterPasswordUnlockMaterial(
            account.UserId,
            password,
            Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray());
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
                unexpectedSuccess.AccountKeySession.Dispose();
        }
    }

    [Fact]
    public void UnlockKey_with_Windows_Hello_forwards_stored_material_and_owner_window()
    {
        using var database = new AccountRepositoryTestDatabase();
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
        var expected = new AccountKeyUnlockResult.Success(new TestAccountKeySession(unlockedUserKey));
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

    private AuthenticatedSession CreateAuthenticatedModel(
        UserId userId,
        string email,
        AccountKeyMaterial keyMaterial,
        SessionRefreshToken refreshToken)
    {
        var jwt = new JwtSecurityToken(claims:
        (Claim[])[
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email)
        ]);
        string accessTokenValue = new JwtSecurityTokenHandler().WriteToken(jwt);

        return new AuthenticatedSession(
            SessionAccessToken.Parse(accessTokenValue),
            refreshToken,
            default,
            new DateTimeOffset(2035, 1, 2, 3, 4, 5, TimeSpan.Zero),
            keyMaterial.ProtectedPrivateKey,
            new MasterPasswordUnlockData(
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

        using var rsa = RSA.Create();
        byte[] privateKey = rsa.ExportPkcs8PrivateKey();
        try
        {
            return keyMaterial with
            {
                Salt = salt,
                ProtectedUserKey = protectedUserKey,
                ProtectedPrivateKey = ProtectedPrivateKey.Create(EncString.Encrypt(privateKey, expectedUserKey))
            };
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKey);
        }
    }

}
