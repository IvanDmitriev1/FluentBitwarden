using BitwardenApi.Tests.Infrastructure;

namespace BitwardenApi.Tests.Tests;

public sealed class IdentityApiTests
{
    [Fact]
    public async Task Password_login_maps_success_payload_to_authenticated_result()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse(AuthenticatedPayload));
        using ServiceProvider provider = TestApiSupport.CreateProvider(identityHandler: handler);
        IIdentityApi identityApi = provider.GetRequiredService<IIdentityApi>();

        SessionTokenResult<TokenAuthenticatedModel> result = await identityApi.AuthenticateWithPasswordAsync(
            new PasswordAuthenticationRequest(TestApiSupport.ClientContext, "user@example.test", "password-hash"),
            TestContext.Current.CancellationToken);

        var success = Assert.IsType<SessionTokenResult<TokenAuthenticatedModel>.Success>(result);
        Assert.Equal(AccessToken.Parse("access-token"), success.Value.AccessToken);
        Assert.Equal(RefreshToken.Parse("refresh-token"), success.Value.RefreshToken);
        Assert.Equal("salt", success.Value.MasterPasswordUnlockModel.Salt);
        Assert.Equal(
            ProtectedPrivateKey.Create(TestApiSupport.ParseEncString(EncodedPrivateKey)),
            success.Value.PrivateKey);
        Assert.Equal(
            new KdfConfig.Pbkdf2(600_000),
            success.Value.MasterPasswordUnlockModel.KdfConfig);
        Assert.Equal(
            ProtectedUserKey.Create(TestApiSupport.ParseEncString(EncodedUserKey)),
            success.Value.MasterPasswordUnlockModel.UserKey);
        TestApiSupport.RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/connect/token", request.RequestUri.AbsolutePath);
        Assert.Equal(
            new Dictionary<string, string>
            {
                ["scope"] = "api offline_access",
                ["client_id"] = "desktop",
                ["deviceType"] = "6",
                ["deviceName"] = "device-name",
                ["deviceIdentifier"] = "device-id",
                ["grant_type"] = "password",
                ["username"] = "user@example.test",
                ["password"] = "password-hash"
            },
            TestApiSupport.ParseForm(request.Content));
    }

    [Fact]
    public async Task Refresh_maps_success_payload_to_refresh_result()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse(RefreshPayload));
        using ServiceProvider provider = TestApiSupport.CreateProvider(identityHandler: handler);
        IIdentityApi identityApi = provider.GetRequiredService<IIdentityApi>();

        DateTimeOffset before = DateTimeOffset.UtcNow;
        SessionTokenResult<TokenRefreshSessionModel> result = await identityApi.RefreshAuthenticationAsync(
            new RefreshAuthenticationRequest(TestApiSupport.ClientContext, RefreshToken.Parse("old-refresh-token")),
            TestContext.Current.CancellationToken);
        DateTimeOffset after = DateTimeOffset.UtcNow;

        var success = Assert.IsType<SessionTokenResult<TokenRefreshSessionModel>.Success>(result);
        Assert.Equal(AccessToken.Parse("new-access-token"), success.Value.AccessToken);
        Assert.Equal(RefreshToken.Parse("new-refresh-token"), success.Value.RefreshToken);
        Assert.InRange(success.Value.ExpiresAt, before.AddSeconds(3600), after.AddSeconds(3600));
        TestApiSupport.RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal("/connect/token", request.RequestUri.AbsolutePath);
        Assert.Equal("refresh_token", TestApiSupport.ParseForm(request.Content)["grant_type"]);
        Assert.Equal("old-refresh-token", TestApiSupport.ParseForm(request.Content)["refresh_token"]);
    }

    [Fact]
    public async Task Bad_request_with_device_verification_flag_maps_to_device_rejection()
    {
        SessionTokenRejection rejection = await TestApiSupport.GetPasswordLoginRejectionAsync(
            "{\"error\":\"invalid_grant\",\"error_description\":\"Verify device\",\"deviceVerificationRequest\":true,\"twoFactorProviders2\":{\"0\":\"Authenticator\"}}" );

        Assert.Equal(SessionTokenRejectionKind.DeviceVerificationRequired, rejection.Kind);
        Assert.Equal("Verify device", rejection.Message);
        Assert.Null(rejection.TwoFactorChallenge);
    }

    [Fact]
    public async Task Bad_request_with_providers_maps_to_two_factor_rejection()
    {
        SessionTokenRejection rejection = await TestApiSupport.GetPasswordLoginRejectionAsync(
            "{\"error\":\"invalid_grant\",\"error_description\":\"Use authenticator\",\"twoFactorProviders2\":{\"0\":\"Authenticator\",\"1\":\"Email\"}}" );

        Assert.Equal(SessionTokenRejectionKind.TwoFactorRequired, rejection.Kind);
        Assert.Equal("Use authenticator", rejection.Message);
        Assert.Equal(
            [IdentityTwoFactorProviderType.Authenticator, IdentityTwoFactorProviderType.Email],
            rejection.TwoFactorChallenge!.Value.Providers.Select(provider => provider.Provider));
    }

    [Fact]
    public async Task Bad_request_without_providers_maps_to_invalid_credentials()
    {
        SessionTokenRejection rejection = await TestApiSupport.GetPasswordLoginRejectionAsync(
            "{\"error\":\"invalid_grant\",\"error_description\":\"Bad credentials\",\"twoFactorProviders2\":{}}" );

        Assert.Equal(SessionTokenRejectionKind.InvalidCredentials, rejection.Kind);
        Assert.Equal("Bad credentials", rejection.Message);
        Assert.Null(rejection.TwoFactorChallenge);
    }

    [Fact]
    public async Task Bad_request_with_missing_providers_maps_to_invalid_credentials()
    {
        SessionTokenRejection rejection = await TestApiSupport.GetPasswordLoginRejectionAsync(
            "{\"error\":\"invalid_grant\",\"error_description\":null}" );

        Assert.Equal(SessionTokenRejectionKind.InvalidCredentials, rejection.Kind);
        Assert.Null(rejection.Message);
        Assert.Null(rejection.TwoFactorChallenge);
    }

    [Fact]
    public async Task Bad_request_with_null_providers_maps_to_invalid_credentials()
    {
        SessionTokenRejection rejection = await TestApiSupport.GetPasswordLoginRejectionAsync(
            "{\"error\":\"invalid_grant\",\"error_description\":\"Bad credentials\",\"twoFactorProviders2\":null}" );

        Assert.Equal(SessionTokenRejectionKind.InvalidCredentials, rejection.Kind);
        Assert.Equal("Bad credentials", rejection.Message);
        Assert.Null(rejection.TwoFactorChallenge);
    }

    [Fact]
    public async Task WebAuthn_assertion_options_are_exposed_by_new_interface()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse("{\"options\":{\"challenge\":\"AQID\",\"timeout\":60000,\"rpId\":\"bitwarden.test\"},\"token\":\"assertion-token\"}"));
        using ServiceProvider provider = TestApiSupport.CreateProvider(identityHandler: handler);
        IWebAuthnIdentityApi webAuthnApi = provider.GetRequiredService<IWebAuthnIdentityApi>();

        WebAuthnLoginAssertionOptionsResult result = await webAuthnApi.GetAuthenticationAssertionOptionsAsync(
            TestApiSupport.ClientContext,
            TestContext.Current.CancellationToken);

        Assert.Equal(new byte[] { 1, 2, 3 }, result.Options.Challenge);
        Assert.Equal("bitwarden.test", result.Options.RpId);
        Assert.Equal(WebAuthnLoginAssertionOptionsToken.Parse("assertion-token"), result.Token);
        Assert.Equal(HttpMethod.Get, handler.Requests.Single().Method);
        Assert.Equal("/accounts/webauthn/assertion-options", handler.Requests.Single().RequestUri.AbsolutePath);
    }

    private const string EncodedPrivateKey = "2.AAAAAAAAAAAAAAAAAAAAAA==|AQ==|AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
    private const string EncodedUserKey = "2.AAECAwQFBgcICQoLDA0ODw==|URUSTubWO/mgdJPbxF260L2ZR++Nqev9R2ivTxISOOs=|9SVSKKsSfeuCZA/eHiHEWz2vSxZR3+x5ubAyGjy284Y=";

    private const string AuthenticatedPayload = $$"""
        {
          "access_token": "access-token",
          "refresh_token": "refresh-token",
          "expires_in": 3600,
          "privateKey": "{{EncodedPrivateKey}}",
          "userDecryptionOptions": {
            "masterPasswordUnlock": {
              "kdf": { "kdfType": 0, "iterations": 600000 },
              "masterKeyEncryptedUserKey": "{{EncodedUserKey}}",
              "salt": "salt"
            }
          }
        }
        """;

    private const string RefreshPayload = "{\"access_token\":\"new-access-token\",\"refresh_token\":\"new-refresh-token\",\"expires_in\":3600}";

}
