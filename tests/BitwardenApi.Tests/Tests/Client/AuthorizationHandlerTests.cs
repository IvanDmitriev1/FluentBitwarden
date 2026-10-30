using BitwardenApi.Client.Http;
using BitwardenApi.Tests.Infrastructure;
using NSubstitute;

namespace BitwardenApi.Tests.Tests;

public sealed class AuthorizationHandlerTests
{
    [Fact]
    public async Task Authorization_handler_resolves_the_token_for_the_attached_account_context()
    {
        BitwardenAccountContext accountContext = TestApiSupport.AccountContext with
        {
            UserId = UserId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
        };
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SessionAccessToken accessToken = SessionAccessToken.Parse("account-specific-token");
        IAccessTokenProvider accessTokenProvider = Substitute.For<IAccessTokenProvider>();
        accessTokenProvider.GetAccessTokenAsync(accountContext)
            .Returns(ValueTask.FromResult(accessToken));

        var terminalHandler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse("{}"));
        using var invoker = new HttpMessageInvoker(new AuthorizationHandler(accessTokenProvider)
        {
            InnerHandler = terminalHandler
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://vault.bitwarden.test/ciphers");
        request.SetBitwardenAccountContext(accountContext);

        using HttpResponseMessage response = await invoker.SendAsync(request, cancellationToken);

        await accessTokenProvider.Received(1).GetAccessTokenAsync(accountContext);
        Assert.Equal(
            ["Bearer account-specific-token"],
            Assert.Single(terminalHandler.Requests).Headers["Authorization"]);
    }

    [Fact]
    public async Task Authorization_handler_rejects_a_request_without_account_context()
    {
        IAccessTokenProvider accessTokenProvider = Substitute.For<IAccessTokenProvider>();
        var terminalHandler = new TestApiSupport.SnapshottingHttpMessageHandler(
            () => TestApiSupport.JsonResponse("{}"));
        using var invoker = new HttpMessageInvoker(new AuthorizationHandler(accessTokenProvider)
        {
            InnerHandler = terminalHandler
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://vault.bitwarden.test/ciphers");

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => invoker.SendAsync(request, TestContext.Current.CancellationToken));

        Assert.Contains("account context", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(terminalHandler.Requests);
    }
}
