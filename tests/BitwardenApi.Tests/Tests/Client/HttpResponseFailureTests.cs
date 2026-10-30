using BitwardenApi.Client.Http;

namespace BitwardenApi.Tests.Tests;

public sealed class HttpResponseFailureTests
{
    [Fact]
    public void Failure_without_an_originating_request_preserves_status_and_body()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("upstream unavailable"),
        };

        BitwardenApiException failure = Assert.Throws<BitwardenApiException>(() =>
            response.EnsureSuccess("Vault test operation", TestContext.Current.CancellationToken));

        Assert.Equal("Vault test operation", failure.Operation);
        Assert.Equal(HttpStatusCode.BadGateway, failure.StatusCode);
        Assert.Equal("upstream unavailable", failure.ResponseBody);
        Assert.Null(failure.RequestUri);
    }
}
