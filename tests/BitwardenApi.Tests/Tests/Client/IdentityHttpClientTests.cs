using BitwardenApi.Tests.Infrastructure;

namespace BitwardenApi.Tests.Tests;

public sealed class IdentityHttpClientTests
{
    [Fact]
    public async Task Identity_client_sends_the_configured_version_and_json_accept_header()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse(
                "{\"error\":\"invalid_grant\",\"error_description\":\"invalid credentials\"}",
                HttpStatusCode.BadRequest));
        using ServiceProvider provider = TestApiSupport.CreateProvider(identityHandler: handler);

        SessionTokenResult<AuthenticatedSession> result = await provider.GetRequiredService<IIdentityApi>()
            .AuthenticateWithPasswordAsync(
                new PasswordAuthenticationRequest(TestApiSupport.ClientContext, "user@example.test", "password-hash"),
                TestContext.Current.CancellationToken);

        Assert.IsType<SessionTokenResult<AuthenticatedSession>.Rejected>(result);
        TestApiSupport.RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(["2026.2.1"], request.Headers["Bitwarden-Client-Version"]);
        Assert.Contains("application/json", request.Headers["Accept"]);
    }

    [Fact]
    public async Task Safe_identity_get_retries_a_transient_server_error()
    {
        int attempts = 0;
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(() =>
            ++attempts == 1
                ? TestApiSupport.JsonResponse("temporarily unavailable", HttpStatusCode.ServiceUnavailable)
                : TestApiSupport.JsonResponse(
                    "{\"options\":{\"challenge\":\"AQID\",\"timeout\":60000,\"rpId\":\"bitwarden.test\"},\"token\":\"assertion-token\"}"));
        using ServiceProvider provider = TestApiSupport.CreateProvider(identityHandler: handler);

        WebAuthnLoginAssertionOptionsResult result = await provider.GetRequiredService<IWebAuthnIdentityApi>()
            .GetAuthenticationAssertionOptionsAsync(
                TestApiSupport.ClientContext,
                TestContext.Current.CancellationToken);

        Assert.Equal("bitwarden.test", result.Options.RpId);
        Assert.Equal(2, attempts);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Identity_token_post_does_not_retry_a_transient_server_error()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse("temporarily unavailable", HttpStatusCode.ServiceUnavailable));
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
    public async Task Identity_request_cancellation_before_send_does_not_reach_transport()
    {
        using var handler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse("{}"));
        using ServiceProvider provider = TestApiSupport.CreateProvider(identityHandler: handler);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GetRequiredService<IIdentityApi>()
            .AuthenticateWithPasswordAsync(
                new PasswordAuthenticationRequest(TestApiSupport.ClientContext, "user@example.test", "password-hash"),
                cancellation.Token));

        Assert.Empty(handler.Requests);
    }
}
