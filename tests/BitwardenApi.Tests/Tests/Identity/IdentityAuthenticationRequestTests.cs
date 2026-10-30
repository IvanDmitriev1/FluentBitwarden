using BitwardenApi.Tests.Infrastructure;

namespace BitwardenApi.Tests.Tests;

public sealed class IdentityAuthenticationRequestTests
{
    [Fact]
    public async Task Password_two_factor_login_sends_the_proof_and_remember_flag()
    {
        TestApiSupport.RecordedRequest request = await CaptureAuthenticationRequestAsync(
            (identityApi, cancellationToken) => identityApi.AuthenticateWithPasswordAndTwoFactorAsync(
                new PasswordTwoFactorAuthenticationRequest(
                    TestApiSupport.ClientContext,
                    "user@example.test",
                    "password-hash",
                    new IdentityTwoFactorProof("123456", IdentityTwoFactorProviderType.Email)),
                cancellationToken));

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
                ["password"] = "password-hash",
                ["twoFactorToken"] = "123456",
                ["twoFactorProvider"] = "1",
                ["twoFactorRemember"] = "1"
            },
            TestApiSupport.ParseForm(request.Content));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("auth-request-id", "auth-request-id")]
    public async Task Device_login_sends_auth_request_only_when_it_is_nonblank(
        string? authRequestValue,
        string? expectedAuthRequest)
    {
        AuthRequestId authRequestId = string.IsNullOrEmpty(authRequestValue)
            ? AuthRequestId.Empty
            : new AuthRequestId(authRequestValue);

        TestApiSupport.RecordedRequest request = await CaptureAuthenticationRequestAsync(
            (identityApi, cancellationToken) => identityApi.AuthenticateWithDeviceAsync(
                new DeviceAuthenticationRequest(
                    TestApiSupport.ClientContext,
                    "user@example.test",
                    "one-time-code",
                    authRequestId),
                cancellationToken));

        Dictionary<string, string> form = new(TestApiSupport.ParseForm(request.Content));
        if (expectedAuthRequest is null)
        {
            Assert.False(form.ContainsKey("authRequest"));
        }
        else
        {
            Assert.Equal(expectedAuthRequest, form["authRequest"]);
        }

        Assert.Equal("password", form["grant_type"]);
        Assert.Equal("user@example.test", form["username"]);
        Assert.Equal("one-time-code", form["password"]);
    }

    [Fact]
    public async Task Authorization_code_login_form_encodes_and_preserves_the_code_fields()
    {
        const string code = "code+/=value";
        const string redirectUri = "https://app.bitwarden.test/callback?state=a+b&next=%2Fvault";
        const string codeVerifier = "verifier-._~+/=";

        TestApiSupport.RecordedRequest request = await CaptureAuthenticationRequestAsync(
            (identityApi, cancellationToken) => identityApi.AuthenticateWithAuthorizationCodeAsync(
                new AuthorizationCodeLoginRequest(
                    TestApiSupport.ClientContext,
                    code,
                    redirectUri,
                    codeVerifier),
                cancellationToken));

        Assert.Equal(
            new Dictionary<string, string>
            {
                ["scope"] = "api offline_access",
                ["client_id"] = "desktop",
                ["deviceType"] = "6",
                ["deviceName"] = "device-name",
                ["deviceIdentifier"] = "device-id",
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["code_verifier"] = codeVerifier
            },
            TestApiSupport.ParseForm(request.Content));
    }

    [Fact]
    public async Task WebAuthn_login_posts_the_assertion_as_base64url_json()
    {
        var deviceResponse = new WebAuthnLoginAssertionResponseRequest
        {
            Id = "credential-id",
            RawId = [1, 2, 3],
            Extensions = new Dictionary<string, string> { ["appid"] = "true" },
            Response = new WebAuthnLoginAssertionResponseData
            {
                AuthenticatorData = [4, 5],
                Signature = [6, 7],
                ClientDataJson = [8, 9],
                UserHandle = [10]
            }
        };

        TestApiSupport.RecordedRequest request = await CaptureAuthenticationRequestAsync(
            (identityApi, cancellationToken) => identityApi.AuthenticateWithWebAuthnAsync(
                new WebAuthnAuthenticationRequest(
                    TestApiSupport.ClientContext,
                    WebAuthnLoginAssertionOptionsToken.Parse("assertion-options-token"),
                    deviceResponse),
                cancellationToken));

        Dictionary<string, string> form = new(TestApiSupport.ParseForm(request.Content));
        Assert.Equal("webauthn", form["grant_type"]);
        Assert.Equal("assertion-options-token", form["token"]);
        using JsonDocument assertion = JsonDocument.Parse(form["deviceResponse"]);
        JsonElement root = assertion.RootElement;
        Assert.Equal("credential-id", root.GetProperty("id").GetString());
        Assert.Equal("AQID", root.GetProperty("rawId").GetString());
        Assert.Equal("public-key", root.GetProperty("type").GetString());
        Assert.Equal("true", root.GetProperty("extensions").GetProperty("appid").GetString());

        JsonElement responseData = root.GetProperty("response");
        Assert.Equal("BAU", responseData.GetProperty("authenticatorData").GetString());
        Assert.Equal("Bgc", responseData.GetProperty("signature").GetString());
        Assert.Equal("CAk", responseData.GetProperty("clientDataJSON").GetString());
        Assert.Equal("Cg", responseData.GetProperty("userHandle").GetString());
    }

    private static async Task<TestApiSupport.RecordedRequest> CaptureAuthenticationRequestAsync(
        Func<IIdentityApi, CancellationToken, Task<SessionTokenResult<AuthenticatedSession>>> authenticate)
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse(
                "{\"error\":\"invalid_grant\",\"error_description\":\"invalid credentials\"}",
                HttpStatusCode.BadRequest));
        using ServiceProvider provider = TestApiSupport.CreateProvider(identityHandler: handler);

        SessionTokenResult<AuthenticatedSession> result = await authenticate(
            provider.GetRequiredService<IIdentityApi>(),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            SessionTokenRejectionKind.InvalidCredentials,
            Assert.IsType<SessionTokenResult<AuthenticatedSession>.Rejected>(result).Error.Kind);
        return Assert.Single(handler.Requests);
    }
}
