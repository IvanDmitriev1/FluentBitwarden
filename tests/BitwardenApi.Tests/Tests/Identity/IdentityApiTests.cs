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

        SessionTokenResult<AuthenticatedSession> result = await identityApi.AuthenticateWithPasswordAsync(
            new PasswordAuthenticationRequest(TestApiSupport.ClientContext, "user@example.test", "password-hash"),
            TestContext.Current.CancellationToken);

        var success = Assert.IsType<SessionTokenResult<AuthenticatedSession>.Success>(result);
        Assert.Equal(SessionAccessToken.Parse("access-token"), success.Value.SessionAccessToken);
        Assert.Equal(SessionRefreshToken.Parse("refresh-token"), success.Value.SessionRefreshToken);
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
        SessionTokenResult<RefreshedSession> result = await identityApi.RefreshAuthenticationAsync(
            new RefreshAuthenticationRequest(TestApiSupport.ClientContext, SessionRefreshToken.Parse("old-refresh-token")),
            TestContext.Current.CancellationToken);
        DateTimeOffset after = DateTimeOffset.UtcNow;

        var success = Assert.IsType<SessionTokenResult<RefreshedSession>.Success>(result);
        Assert.Equal(SessionAccessToken.Parse("new-access-token"), success.Value.SessionAccessToken);
        Assert.Equal(SessionRefreshToken.Parse("new-refresh-token"), success.Value.SessionRefreshToken);
        Assert.InRange(success.Value.ExpiresAt, before.AddSeconds(3600), after.AddSeconds(3600));
        TestApiSupport.RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal("/connect/token", request.RequestUri.AbsolutePath);
        Assert.Equal("refresh_token", TestApiSupport.ParseForm(request.Content)["grant_type"]);
        Assert.Equal("old-refresh-token", TestApiSupport.ParseForm(request.Content)["refresh_token"]);
    }

    [Fact]
    public async Task Refresh_rejects_a_null_success_payload()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse("null"));
        using ServiceProvider provider = TestApiSupport.CreateProvider(identityHandler: handler);

        await Assert.ThrowsAsync<InvalidDataException>(() => provider.GetRequiredService<IIdentityApi>()
            .RefreshAuthenticationAsync(
                new RefreshAuthenticationRequest(TestApiSupport.ClientContext, SessionRefreshToken.Parse("refresh-token")),
                TestContext.Current.CancellationToken));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Authentication_rejects_a_null_error_payload()
    {
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            TestApiSupport.GetPasswordLoginRejectionAsync("null"));
    }

    [Fact]
    public async Task Authentication_maps_argon2_unlock_settings_and_optional_two_factor_token()
    {
        string payload = AuthenticatedPayload
            .Replace("\"refresh_token\": \"refresh-token\",", "\"refresh_token\": \"refresh-token\",\n  \"twoFactorToken\": \"remember-token\",")
            .Replace("\"kdfType\": 0, \"iterations\": 600000", "\"kdfType\": 1, \"iterations\": 3, \"memory\": 64, \"parallelism\": 4");
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse(payload));
        using ServiceProvider provider = TestApiSupport.CreateProvider(identityHandler: handler);

        SessionTokenResult<AuthenticatedSession> result = await provider.GetRequiredService<IIdentityApi>()
            .AuthenticateWithPasswordAsync(
                new PasswordAuthenticationRequest(TestApiSupport.ClientContext, "user@example.test", "password-hash"),
                TestContext.Current.CancellationToken);

        var success = Assert.IsType<SessionTokenResult<AuthenticatedSession>.Success>(result);
        Assert.Equal(TwoFactorToken.Parse("remember-token"), success.Value.TwoFactorToken);
        Assert.Equal(new KdfConfig.Argon2Id(3, 64, 4), success.Value.MasterPasswordUnlockModel.KdfConfig);
    }

    [Fact]
    public async Task Authentication_rejects_an_unsupported_kdf_type()
    {
        string payload = AuthenticatedPayload.Replace("\"kdfType\": 0", "\"kdfType\": 99");
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse(payload));
        using ServiceProvider provider = TestApiSupport.CreateProvider(identityHandler: handler);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => provider.GetRequiredService<IIdentityApi>()
            .AuthenticateWithPasswordAsync(
                new PasswordAuthenticationRequest(TestApiSupport.ClientContext, "user@example.test", "password-hash"),
                TestContext.Current.CancellationToken));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Authentication_rejects_a_null_success_payload()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse("null"));
        using ServiceProvider provider = TestApiSupport.CreateProvider(identityHandler: handler);

        await Assert.ThrowsAsync<InvalidDataException>(() => provider.GetRequiredService<IIdentityApi>()
            .AuthenticateWithPasswordAsync(
                new PasswordAuthenticationRequest(TestApiSupport.ClientContext, "user@example.test", "password-hash"),
                TestContext.Current.CancellationToken));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Authentication_throws_for_non_bad_request_error_status()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse("{\"error\":\"server_error\"}", HttpStatusCode.ServiceUnavailable));
        using ServiceProvider provider = TestApiSupport.CreateProvider(identityHandler: handler);

        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(() => provider
            .GetRequiredService<IIdentityApi>()
            .AuthenticateWithPasswordAsync(
                new PasswordAuthenticationRequest(TestApiSupport.ClientContext, "user@example.test", "password-hash"),
                TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
        Assert.Single(handler.Requests);
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
    public async Task Bad_request_ignores_unknown_providers_and_skips_provider_metadata()
    {
        SessionTokenRejection rejection = await TestApiSupport.GetPasswordLoginRejectionAsync(
            "{\"error\":\"invalid_grant\",\"error_description\":\"Use supported methods\",\"twoFactorProviders2\":{\"0\":{\"name\":\"Authenticator\"},\"7\":null,\"x\":null,\"5\":{}}}");

        Assert.Equal(SessionTokenRejectionKind.TwoFactorRequired, rejection.Kind);
        Assert.Equal(
            [IdentityTwoFactorProviderType.Authenticator, IdentityTwoFactorProviderType.Remember],
            rejection.TwoFactorChallenge!.Value.Providers.Select(provider => provider.Provider));
    }

    [Fact]
    public async Task Bad_request_with_device_flag_and_providers_requires_device_verification()
    {
        SessionTokenRejection rejection = await TestApiSupport.GetPasswordLoginRejectionAsync(
            "{\"error\":\"invalid_grant\",\"error_description\":\"Verify device first\",\"deviceVerificationRequest\":true,\"twoFactorProviders2\":{\"0\":null}}");

        Assert.Equal(SessionTokenRejectionKind.DeviceVerificationRequired, rejection.Kind);
        Assert.Equal("Verify device first", rejection.Message);
        Assert.Null(rejection.TwoFactorChallenge);
    }

    [Fact]
    public async Task Bad_request_with_malformed_provider_container_is_rejected_as_invalid_json()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse(
                "{\"error\":\"invalid_grant\",\"error_description\":\"bad response\",\"twoFactorProviders2\":[]}",
                HttpStatusCode.BadRequest));
        using ServiceProvider provider = TestApiSupport.CreateProvider(identityHandler: handler);

        await Assert.ThrowsAsync<JsonException>(() => provider.GetRequiredService<IIdentityApi>().AuthenticateWithPasswordAsync(
            new PasswordAuthenticationRequest(TestApiSupport.ClientContext, "user@example.test", "password-hash"),
            TestContext.Current.CancellationToken));

        Assert.Single(handler.Requests);
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
        Assert.Equal(60_000u, result.Options.TimeoutMilliseconds);
        Assert.Equal("bitwarden.test", result.Options.RpId);
        Assert.Equal(WebAuthnLoginAssertionOptionsToken.Parse("assertion-token"), result.Token);
        Assert.Equal(HttpMethod.Get, handler.Requests.Single().Method);
        Assert.Equal("/accounts/webauthn/assertion-options", handler.Requests.Single().RequestUri.AbsolutePath);
    }

    [Fact]
    public async Task WebAuthn_options_failure_preserves_operation_status_body_and_request_uri()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse("service unavailable", HttpStatusCode.ServiceUnavailable));
        using ServiceProvider provider = TestApiSupport.CreateProvider(identityHandler: handler);

        BitwardenApiException exception = await Assert.ThrowsAsync<BitwardenApiException>(() => provider
            .GetRequiredService<IWebAuthnIdentityApi>()
            .GetAuthenticationAssertionOptionsAsync(
                TestApiSupport.ClientContext,
                TestContext.Current.CancellationToken));

        Assert.Equal("Identity get WebAuthn assertion options", exception.Operation);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
        Assert.Equal("service unavailable", exception.ResponseBody);
        Assert.Equal("https://identity.bitwarden.test/accounts/webauthn/assertion-options", exception.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task WebAuthn_options_failure_with_empty_body_keeps_an_empty_response_body()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse(string.Empty, HttpStatusCode.NotFound));
        using ServiceProvider provider = TestApiSupport.CreateProvider(identityHandler: handler);

        BitwardenApiException exception = await Assert.ThrowsAsync<BitwardenApiException>(() => provider
            .GetRequiredService<IWebAuthnIdentityApi>()
            .GetAuthenticationAssertionOptionsAsync(
                TestApiSupport.ClientContext,
                TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        Assert.Equal(string.Empty, exception.ResponseBody);
        Assert.Equal(
            "Identity get WebAuthn assertion options failed with HTTP 404 (NotFound).",
            exception.Message);
    }

    [Fact]
    public async Task WebAuthn_options_reject_a_null_success_payload()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() => TestApiSupport.JsonResponse("null"));
        using ServiceProvider provider = TestApiSupport.CreateProvider(identityHandler: handler);

        await Assert.ThrowsAsync<InvalidDataException>(() => provider
            .GetRequiredService<IWebAuthnIdentityApi>()
            .GetAuthenticationAssertionOptionsAsync(
                TestApiSupport.ClientContext,
                TestContext.Current.CancellationToken));

        Assert.Single(handler.Requests);
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
