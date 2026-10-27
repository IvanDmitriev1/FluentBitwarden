using System.Security.Cryptography;
using BitwardenApi.Primitives;
using BitwardenApi.Vault.Ciphers;

namespace BitwardenApi.Tests.Tests;

public sealed class CipherRequestFactoryTests
{
    [Fact]
    public void Build_request_wraps_cipher_key_and_encrypts_login_fields()
    {
        using var userKey = new UnlockedUserKey(
            UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            [.. Enumerable.Range(0, 64).Select(static value => (byte)value)]);
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Name = "Example login";
        cipher.Notes = "private note";
        cipher.Username = "alice";
        cipher.Password = "password";
        cipher.Uris.Add(new LoginUri { Value = "https://example.test", Match = LoginUri.MatchType.Exact });

        CipherRequest request = CipherRequestFactory.BuildRequest(userKey, cipher);

        byte[] cipherKey = new byte[request.Key.MaxPlaintextByteCount];
        try
        {
            int length = request.Key.DecodeTo(userKey.Key, cipherKey);
            Assert.Equal(64, length);
            Assert.Equal(userKey.UserId.Value, request.EncryptedFor);
            Assert.Equal(CipherType.Login, request.Type);
            Assert.Null(request.FolderId);
            Assert.Null(request.LastKnownRevisionDate);
            Assert.Equal("Example login", request.Name.Decode(cipherKey));
            Assert.Equal("private note", request.Notes.Decode(cipherKey));
            CipherLoginRequest login = Assert.IsType<CipherLoginRequest>(request.Login);
            Assert.Equal("alice", login.Username.Decode(cipherKey));
            Assert.Equal("password", login.Password.Decode(cipherKey));
            Assert.Equal("https://example.test", Assert.Single(login.Uris).Uri.Decode(cipherKey));
            Assert.Equal((int)LoginUri.MatchType.Exact, login.Uris[0].Match);
            Assert.True(login.Totp.IsEmpty);
            Assert.Empty(login.Fido2Credentials);
            Assert.Null(request.Card);
            Assert.Null(request.Identity);
            Assert.Null(request.SecureNote);
            Assert.Null(request.SshKey);
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
        var cipher = (SecureNoteCipher)Cipher.CreateBlankCipher(CipherType.SecureNote);
        cipher.Id = CipherId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        cipher.RevisionDate = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        cipher.Reprompt = true;

        CipherRequest request = CipherRequestFactory.BuildRequest(userKey, cipher);

        Assert.Equal(cipher.RevisionDate.UtcDateTime, request.LastKnownRevisionDate);
        Assert.Equal(1, request.Reprompt);
        Assert.Equal(0, Assert.IsType<CipherSecureNoteRequest>(request.SecureNote).Type);
        Assert.True(request.Name.IsEmpty);
        Assert.True(request.Notes.IsEmpty);
        Assert.Null(request.Login);
        Assert.Null(request.Card);
        Assert.Null(request.Identity);
        Assert.Null(request.SshKey);
    }

    [Fact]
    public void Build_card_request_encrypts_each_card_field_and_preserves_folder_and_revision()
    {
        using var userKey = CreateUserKey();
        var cipher = (CardCipher)Cipher.CreateBlankCipher(CipherType.Card);
        cipher.Id = CipherId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        cipher.FolderId = FolderId.Parse("folder-id");
        cipher.RevisionDate = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(3));
        cipher.Favorite = true;
        cipher.CardholderName = "Alice Example";
        cipher.Brand = "VISA";
        cipher.Number = "4111111111111111";
        cipher.ExpMonth = "03";
        cipher.ExpYear = "2030";
        cipher.Code = "123";

        CipherRequest request = CipherRequestFactory.BuildRequest(userKey, cipher);
        byte[] cipherKey = UnwrapKey(request, userKey);
        try
        {
            Assert.Equal(CipherType.Card, request.Type);
            Assert.True(request.Favorite);
            Assert.Equal("folder-id", request.FolderId);
            Assert.Equal(cipher.RevisionDate.UtcDateTime, request.LastKnownRevisionDate);
            Assert.Null(request.Login);
            Assert.Null(request.Identity);
            Assert.Null(request.SecureNote);
            Assert.Null(request.SshKey);
            CipherCardRequest card = Assert.IsType<CipherCardRequest>(request.Card);
            Assert.Equal("Alice Example", card.CardholderName.Decode(cipherKey));
            Assert.Equal("VISA", card.Brand.Decode(cipherKey));
            Assert.Equal("4111111111111111", card.Number.Decode(cipherKey));
            Assert.Equal("03", card.ExpMonth.Decode(cipherKey));
            Assert.Equal("2030", card.ExpYear.Decode(cipherKey));
            Assert.Equal("123", card.Code.Decode(cipherKey));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(cipherKey);
        }
    }

    [Fact]
    public void Build_identity_request_encrypts_each_identity_field()
    {
        using var userKey = CreateUserKey();
        var cipher = (IdentityCipher)Cipher.CreateBlankCipher(CipherType.Identity);
        cipher.Title = "Dr.";
        cipher.FirstName = "Alice";
        cipher.MiddleName = "M";
        cipher.LastName = "Example";
        cipher.Address1 = "1 Main St";
        cipher.Address2 = "Unit 2";
        cipher.Address3 = "Attn: Alice";
        cipher.City = "Exampletown";
        cipher.State = "WA";
        cipher.PostalCode = "98000";
        cipher.Country = "US";
        cipher.Company = "Example, Inc.";
        cipher.Email = "alice@example.test";
        cipher.Phone = "+1-555-0100";
        cipher.Ssn = "123-45-6789";
        cipher.Username = "alice";
        cipher.PassportNumber = "P1234567";
        cipher.LicenseNumber = "D1234567";

        CipherRequest request = CipherRequestFactory.BuildRequest(userKey, cipher);
        byte[] cipherKey = UnwrapKey(request, userKey);
        try
        {
            Assert.Equal(CipherType.Identity, request.Type);
            Assert.Null(request.Login);
            Assert.Null(request.Card);
            Assert.Null(request.SecureNote);
            Assert.Null(request.SshKey);
            CipherIdentityRequest identity = Assert.IsType<CipherIdentityRequest>(request.Identity);
            Assert.Equal("Dr.", identity.Title.Decode(cipherKey));
            Assert.Equal("Alice", identity.FirstName.Decode(cipherKey));
            Assert.Equal("M", identity.MiddleName.Decode(cipherKey));
            Assert.Equal("Example", identity.LastName.Decode(cipherKey));
            Assert.Equal("1 Main St", identity.Address1.Decode(cipherKey));
            Assert.Equal("Unit 2", identity.Address2.Decode(cipherKey));
            Assert.Equal("Attn: Alice", identity.Address3.Decode(cipherKey));
            Assert.Equal("Exampletown", identity.City.Decode(cipherKey));
            Assert.Equal("WA", identity.State.Decode(cipherKey));
            Assert.Equal("98000", identity.PostalCode.Decode(cipherKey));
            Assert.Equal("US", identity.Country.Decode(cipherKey));
            Assert.Equal("Example, Inc.", identity.Company.Decode(cipherKey));
            Assert.Equal("alice@example.test", identity.Email.Decode(cipherKey));
            Assert.Equal("+1-555-0100", identity.Phone.Decode(cipherKey));
            Assert.Equal("123-45-6789", identity.Ssn.Decode(cipherKey));
            Assert.Equal("alice", identity.Username.Decode(cipherKey));
            Assert.Equal("P1234567", identity.PassportNumber.Decode(cipherKey));
            Assert.Equal("D1234567", identity.LicenseNumber.Decode(cipherKey));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(cipherKey);
        }
    }

    [Fact]
    public void Build_ssh_key_request_encrypts_private_public_and_fingerprint_values()
    {
        using var userKey = CreateUserKey();
        var cipher = (SshKeyCipher)Cipher.CreateBlankCipher(CipherType.SshKey);
        cipher.PrivateKey = "private-key";
        cipher.PublicKey = OpenSshPublicKey.CreateUnparsed("ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAA");
        cipher.KeyFingerprint = "SHA256:fingerprint";

        CipherRequest request = CipherRequestFactory.BuildRequest(userKey, cipher);
        byte[] cipherKey = UnwrapKey(request, userKey);
        try
        {
            Assert.Equal(CipherType.SshKey, request.Type);
            Assert.Null(request.Login);
            Assert.Null(request.Card);
            Assert.Null(request.Identity);
            Assert.Null(request.SecureNote);
            CipherSshKeyRequest sshKey = Assert.IsType<CipherSshKeyRequest>(request.SshKey);
            Assert.Equal("private-key", sshKey.PrivateKey.Decode(cipherKey));
            Assert.Equal("ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAA", sshKey.PublicKey.Decode(cipherKey));
            Assert.Equal("SHA256:fingerprint", sshKey.KeyFingerprint.Decode(cipherKey));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(cipherKey);
        }
    }

    [Theory]
    [InlineData(false, "false")]
    [InlineData(true, "true")]
    public void Build_login_request_encrypts_totp_and_all_fido2_fields(bool discoverable, string expectedDiscoverable)
    {
        using var userKey = CreateUserKey();
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Totp = new TotpValue(new TotpValue.State(
            OtpNet.OtpType.Totp,
            OtpNet.OtpHashMode.Sha256,
            "1234567890"u8.ToArray(),
            8,
            45));
        Guid credentialId = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
        DateTimeOffset creationDate = new(2026, 2, 3, 4, 5, 6, TimeSpan.FromHours(2));
        cipher.Fido2Credential = new Fido2Credential
        {
            CredentialId = credentialId.ToByteArray(bigEndian: true),
            KeyType = Fido2CredentialKeyType.PublicKey,
            KeyAlgorithm = Fido2CredentialKeyAlgorithm.Ecdsa,
            KeyCurve = Fido2CredentialKeyCurve.P256,
            KeyValue = [251, 255],
            RpId = "example.test",
            RpName = "Example",
            UserHandle = [255, 254, 253],
            UserName = "alice",
            UserDisplayName = "Alice Example",
            Counter = uint.MaxValue,
            Discoverable = discoverable,
            CreationDate = creationDate
        };

        CipherRequest request = CipherRequestFactory.BuildRequest(userKey, cipher);
        byte[] cipherKey = UnwrapKey(request, userKey);
        try
        {
            CipherLoginRequest login = Assert.IsType<CipherLoginRequest>(request.Login);
            Assert.Equal(
                "otpauth://totp/?secret=GEZDGNBVGY3TQOJQ&algorithm=SHA256&digits=8&period=45",
                login.Totp.Decode(cipherKey));
            CipherFido2CredentialRequest credential = Assert.Single(login.Fido2Credentials);
            Assert.Equal(credentialId.ToString(), credential.CredentialId.Decode(cipherKey));
            Assert.Equal("public-key", credential.KeyType.Decode(cipherKey));
            Assert.Equal("ECDSA", credential.KeyAlgorithm.Decode(cipherKey));
            Assert.Equal("P-256", credential.KeyCurve.Decode(cipherKey));
            Assert.Equal("-_8", credential.KeyValue.Decode(cipherKey));
            Assert.Equal("example.test", credential.RpId.Decode(cipherKey));
            Assert.Equal("Example", credential.RpName.Decode(cipherKey));
            Assert.Equal("__79", credential.UserHandle.Decode(cipherKey));
            Assert.Equal("alice", credential.UserName.Decode(cipherKey));
            Assert.Equal("Alice Example", credential.UserDisplayName.Decode(cipherKey));
            Assert.Equal("4294967295", credential.Counter.Decode(cipherKey));
            Assert.Equal(expectedDiscoverable, credential.Discoverable.Decode(cipherKey));
            Assert.Equal(creationDate.UtcDateTime, credential.CreationDate);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(cipherKey);
        }
    }

    [Fact]
    public void Create_blank_cipher_supports_each_wire_type_and_rejects_unknown_types()
    {
        Assert.IsType<LoginCipher>(Cipher.CreateBlankCipher(CipherType.Login));
        Assert.IsType<SecureNoteCipher>(Cipher.CreateBlankCipher(CipherType.SecureNote));
        Assert.IsType<CardCipher>(Cipher.CreateBlankCipher(CipherType.Card));
        Assert.IsType<IdentityCipher>(Cipher.CreateBlankCipher(CipherType.Identity));
        Assert.IsType<SshKeyCipher>(Cipher.CreateBlankCipher(CipherType.SshKey));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cipher.CreateBlankCipher((CipherType)0));
    }

    private static UnlockedUserKey CreateUserKey() => new(
        UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        [.. Enumerable.Range(0, 64).Select(static value => (byte)value)]);

    private static byte[] UnwrapKey(CipherRequest request, UnlockedUserKey userKey)
    {
        byte[] cipherKey = new byte[request.Key.MaxPlaintextByteCount];
        try
        {
            int length = request.Key.DecodeTo(userKey.Key, cipherKey);
            Assert.Equal(64, length);
            return cipherKey;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(cipherKey);
            throw;
        }
    }
}
