using System.Security.Cryptography;
using BitwardenApi.Vault.Cryptography;
using BitwardenApi.Vault.Items;
using BitwardenApi.Vault.Items.Contracts;

namespace BitwardenApi.Tests.Tests;

public sealed class VaultCipherRequestFactoryTests
{
    [Fact]
    public void Build_request_wraps_cipher_key_and_encrypts_login_fields()
    {
        using var userKey = new UnlockedUserKey(
            UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            [.. Enumerable.Range(0, 64).Select(static value => (byte)value)]);
        var cipher = (LoginVaultCipher)VaultCipher.CreateBlankCipher(VaultCipherType.Login);
        cipher.Name = "Example login";
        cipher.Notes = "private note";
        cipher.Username = "alice";
        cipher.Password = "password";
        cipher.Uris.Add(new LoginUri { Value = "https://example.test", Match = LoginUri.MatchType.Exact });

        VaultCipherRequest request = VaultCipherRequestFactory.BuildRequest(userKey, cipher);

        byte[] cipherKey = new byte[request.Key.MaxPlaintextByteCount];
        try
        {
            int length = request.Key.DecodeTo(userKey.Key, cipherKey);
            Assert.Equal(64, length);
            Assert.Equal(userKey.UserId.Value, request.EncryptedFor);
            Assert.Equal(VaultCipherType.Login, request.Type);
            Assert.Null(request.FolderId);
            Assert.Null(request.LastKnownRevisionDate);
            Assert.Equal("Example login", request.Name.Decode(cipherKey));
            Assert.Equal("private note", request.Notes.Decode(cipherKey));
            CipherLoginRequest login = Assert.IsType<CipherLoginRequest>(request.Login);
            Assert.Equal("alice", login.Username.Decode(cipherKey));
            Assert.Equal("password", login.Password.Decode(cipherKey));
            Assert.Equal("https://example.test", Assert.Single(login.Uris).Uri.Decode(cipherKey));
            Assert.Equal((int)LoginUri.MatchType.Exact, login.Uris[0].Match);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(cipherKey);
        }
    }

    [Fact]
    public void Build_request_includes_the_revision_guard_for_an_existing_cipher()
    {
        using var userKey = new UnlockedUserKey(
            UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            [.. Enumerable.Range(0, 64).Select(static value => (byte)value)]);
        var cipher = (SecureNoteVaultCipher)VaultCipher.CreateBlankCipher(VaultCipherType.SecureNote);
        cipher.Id = CipherId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        cipher.RevisionDate = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        cipher.Reprompt = true;

        VaultCipherRequest request = VaultCipherRequestFactory.BuildRequest(userKey, cipher);

        Assert.Equal(cipher.RevisionDate.UtcDateTime, request.LastKnownRevisionDate);
        Assert.Equal(1, request.Reprompt);
        Assert.Equal(0, Assert.IsType<CipherSecureNoteRequest>(request.SecureNote).Type);
        Assert.True(request.Name.IsEmpty);
    }
}
