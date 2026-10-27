using BitwardenApi.Primitives;

namespace BitwardenApi.Tests.Tests.Primitives;

public sealed class OpenSshPublicKeyTests
{
    [Fact]
    public void TryParse_preserves_the_input_and_decodes_the_key_blob()
    {
        const string rawKey = "ssh-ed25519 AQIDBA== test comment";

        bool success = OpenSshPublicKey.TryParse(rawKey, out OpenSshPublicKey key);

        Assert.True(success);
        Assert.Equal(rawKey, key.RawKey);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, key.KeyBlob);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ssh-ed25519")]
    [InlineData("ssh-ed25519 not-base64!")]
    public void TryParse_rejects_empty_missing_or_invalid_key_text(string rawKey)
    {
        bool success = OpenSshPublicKey.TryParse(rawKey, out _);

        Assert.False(success);
    }

    [Fact]
    public void Empty_and_unparsed_values_keep_their_declared_shape()
    {
        Assert.Equal(string.Empty, OpenSshPublicKey.Empty.RawKey);
        Assert.Empty(OpenSshPublicKey.Empty.KeyBlob);

        OpenSshPublicKey unparsed = OpenSshPublicKey.CreateUnparsed("raw public key");
        Assert.Equal("raw public key", unparsed.RawKey);
        Assert.Empty(unparsed.KeyBlob);
    }
}
