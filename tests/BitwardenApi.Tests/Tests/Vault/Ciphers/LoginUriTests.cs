namespace BitwardenApi.Tests.Tests.Vault.Ciphers;

public sealed class LoginUriTests
{
    [Fact]
    public void Http_is_a_web_uri_and_ftp_remains_an_absolute_nonweb_uri()
    {
        var loginUri = new LoginUri { Value = "http://example.test/path" };
        Assert.True(loginUri.IsWebUri);
        Assert.True(loginUri.TryGetWebUri(out Uri? webUri));
        Assert.Equal(new Uri("http://example.test/path"), webUri);

        loginUri.Value = "ftp://example.test/path";
        Assert.False(loginUri.IsWebUri);
        Assert.True(loginUri.TryGetAbsoluteUri(out Uri? absoluteUri));
        Assert.Equal(new Uri("ftp://example.test/path"), absoluteUri);
    }

    [Fact]
    public void Absolute_http_value_is_trimmed_and_returned_as_a_web_uri()
    {
        var loginUri = new LoginUri { Value = "  https://example.test/path  " };

        Assert.Equal("https://example.test/path", loginUri.Value);
        Assert.Equal("https://example.test/path", loginUri.ToString());
        Assert.True(loginUri.IsWebUri);
        Assert.True(loginUri.TryGetAbsoluteUri(out Uri? absoluteUri));
        Assert.Equal(new Uri("https://example.test/path"), absoluteUri);
        Assert.True(loginUri.TryGetWebUri(out Uri? webUri));
        Assert.Equal(absoluteUri, webUri);
    }

    [Fact]
    public void Host_value_gets_the_default_https_scheme()
    {
        var loginUri = new LoginUri { Value = "example.test/path" };

        Assert.False(loginUri.IsWebUri);
        Assert.False(loginUri.TryGetAbsoluteUri(out _));
        Assert.True(loginUri.TryGetWebUri(out Uri? webUri));
        Assert.Equal(new Uri("https://example.test/path"), webUri);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("not a valid host/%")]
    public void Blank_or_invalid_values_do_not_produce_a_web_uri(string value)
    {
        var loginUri = new LoginUri { Value = value };

        Assert.False(loginUri.TryGetWebUri(out Uri? webUri));
        Assert.Null(webUri);
    }
}
