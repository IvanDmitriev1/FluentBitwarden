using System.Security.Cryptography;
using BitwardenApi.Primitives;
using BitwardenApi.Vault.Ciphers;

using System.Buffers;
using System.Text.Json.Serialization;

using BitwardenApi.Vault.Attachments.Responses;
using BitwardenApi.Vault.Ciphers.Responses;

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
            ReadOnlySpan<byte> effectiveCipherKey = cipherKey.AsSpan(..length);
            Assert.Equal(userKey.UserId.Value, request.EncryptedFor);
            Assert.Equal(CipherType.Login, request.Type);
            Assert.True(request.FolderId.IsEmpty);
            Assert.Null(request.LastKnownRevisionDate);
            Assert.Equal("Example login", request.Name.Decode(effectiveCipherKey));
            Assert.Equal("private note", request.Notes.Decode(effectiveCipherKey));
            CipherLoginRequest login = Assert.IsType<CipherLoginRequest>(request.Login);
            Assert.Equal("alice", login.Username.Decode(effectiveCipherKey));
            Assert.Equal("password", login.Password.Decode(effectiveCipherKey));
            Assert.Equal("https://example.test", Assert.Single(login.Uris).Uri.Decode(effectiveCipherKey));
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
    public void Build_update_request_keeps_the_original_key_and_opaque_encrypted_data()
    {
        using var userKey = CreateUserKey();
        byte[] itemKey = [.. Enumerable.Range(65, 64).Select(static value => (byte)value)];
        EncString protectedKey = EncString.Encrypt(itemKey, userKey.Key);
        byte[] fields = CreateFields(itemKey);
        byte[] history = CreatePasswordHistory(itemKey);
        byte[] data = CreateRichLoginData(itemKey, fields, history, unsupportedTotp: true);
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = CipherId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        cipher.RevisionDate = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        cipher.Name = "Updated name";
        cipher.Notes = "Updated notes";
        cipher.Username = "updated-user";
        cipher.Password = "updated-password";
        cipher.Uris.Add(new LoginUri { Value = "https://second.test", Match = LoginUri.MatchType.Host });
        cipher.Uris.Add(new LoginUri { Value = "https://example.test", Match = LoginUri.MatchType.Exact });
        byte[] attachmentKey = [.. Enumerable.Range(151, 64).Select(static value => (byte)value)];
        CipherResponse baseline = ExistingCipher(
            cipher,
            protectedKey,
            data,
            archived: true,
            attachments:
            [
                new AttachmentDownloadResponse
                {
                    Id = AttachmentId.Parse("existing-attachment"),
                    Url = "https://example.test/attachment",
                    EncryptedFileName = EncString.Encrypt("old-file.txt", itemKey),
                    ProtectedAttachmentKey = EncString.Encrypt(attachmentKey, itemKey),
                    Size = FileSize.FromBytes(12)
                }
            ]);

        CipherRequest request = CipherRequestFactory.BuildRequest(userKey, cipher, in baseline);
        byte[] requestKey = UnwrapKey(request, userKey);
        try
        {
            Assert.Equal(protectedKey, request.Key);
            Assert.Equal(cipher.RevisionDate.UtcDateTime, request.LastKnownRevisionDate);
            Assert.Equal(baseline.ArchivedDate?.UtcDateTime, request.ArchivedDate);
            Assert.Equal("Updated name", request.Name.Decode(requestKey));
            AttachmentDownloadResponse attachment = Assert.Single(baseline.Attachments!);
            Assert.Equal("old-file.txt", attachment.EncryptedFileName.Decode(requestKey));
            byte[] unwrappedAttachmentKey = new byte[64];
            try
            {
                int bytesWritten = attachment.ProtectedAttachmentKey.DecodeTo(requestKey, unwrappedAttachmentKey);
                Assert.Equal(64, bytesWritten);
                Assert.Equal(attachmentKey, unwrappedAttachmentKey);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(unwrappedAttachmentKey);
            }

            using JsonDocument updated = JsonDocument.Parse(request.Data!);
            JsonElement root = updated.RootElement;
            Assert.Equal("Updated name", Decode(root.GetProperty("name"), requestKey));
            Assert.Equal("Updated notes", Decode(root.GetProperty("notes"), requestKey));
            Assert.Equal("updated-user", Decode(root.GetProperty("username"), requestKey));
            Assert.Equal("updated-password", Decode(root.GetProperty("password"), requestKey));
            Assert.Equal(System.Text.Encoding.UTF8.GetString(fields), root.GetProperty("fields").GetRawText());
            Assert.Equal(System.Text.Encoding.UTF8.GetString(history), root.GetProperty("passwordHistory").GetRawText());
            Assert.Equal("unsupported format", Decode(root.GetProperty("totp"), requestKey));
            Assert.Equal(2, root.GetProperty("fido2Credentials").GetArrayLength());
            Assert.Equal("first-future-credential-data", root.GetProperty("fido2Credentials")[0].GetProperty("futureData").GetString());
            Assert.Equal("second-future-credential-data", root.GetProperty("fido2Credentials")[1].GetProperty("futureData").GetString());
            Assert.Equal("https://second.test", Decode(root.GetProperty("uris")[0].GetProperty("uri"), requestKey));
            Assert.Equal("keep-second-checksum", root.GetProperty("uris")[0].GetProperty("checksum").GetString());
            Assert.Equal("https://example.test", Decode(root.GetProperty("uris")[1].GetProperty("uri"), requestKey));
            Assert.Equal("keep-checksum", root.GetProperty("uris")[1].GetProperty("checksum").GetString());
            Assert.Equal("preserve-me", root.GetProperty("futureLoginProperty").GetProperty("opaque").GetString());
        }
        finally
        {
            CryptographicOperations.ZeroMemory(requestKey);
            CryptographicOperations.ZeroMemory(itemKey);
            CryptographicOperations.ZeroMemory(attachmentKey);
        }
    }

    [Fact]
    public void Build_update_request_uses_the_user_key_for_legacy_ciphers_and_allows_totp_clear()
    {
        using var userKey = CreateUserKey();
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = CipherId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        cipher.Name = "Legacy edit";
        byte[] data = CreateRichLoginData(userKey.Key, "[]"u8.ToArray(), "[]"u8.ToArray(), unsupportedTotp: false);
        CipherResponse baseline = ExistingCipher(cipher, EncString.Empty, data);

        CipherRequest request = CipherRequestFactory.BuildRequest(userKey, cipher, in baseline);
        Assert.True(request.Key.IsEmpty);
        using JsonDocument updated = JsonDocument.Parse(request.Data!);
        Assert.Equal("Legacy edit", Decode(updated.RootElement.GetProperty("name"), userKey.Key));
        Assert.Equal(JsonValueKind.Null, updated.RootElement.GetProperty("totp").ValueKind);
    }

    [Fact]
    public void Build_update_request_rejects_malformed_original_data()
    {
        using var userKey = CreateUserKey();
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = CipherId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        CipherResponse baseline = ExistingCipher(cipher, EncString.Empty, "not-json"u8.ToArray());

        Assert.ThrowsAny<JsonException>(() => CipherRequestFactory.BuildRequest(userKey, cipher, in baseline));
    }

    [Fact]
    public void Build_update_request_rejects_uri_edits_when_the_original_uri_has_unmodeled_metadata()
    {
        using var userKey = CreateUserKey();
        byte[] itemKey = [.. Enumerable.Range(65, 64).Select(static value => (byte)value)];
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = CipherId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        cipher.Uris.Add(new LoginUri { Value = "https://changed.test" });
        CipherResponse baseline = ExistingCipher(
            cipher,
            EncString.Encrypt(itemKey, userKey.Key),
            CreateRichLoginData(itemKey, "[]"u8.ToArray(), "[]"u8.ToArray(), unsupportedTotp: true));

        Assert.Throws<InvalidOperationException>(() => CipherRequestFactory.BuildRequest(userKey, cipher, in baseline));
        CryptographicOperations.ZeroMemory(itemKey);
    }

    [Fact]
    public void Build_update_request_allows_adding_a_uri_when_the_baseline_uri_value_is_null()
    {
        using var userKey = CreateUserKey();
        byte[] itemKey = [.. Enumerable.Range(65, 64).Select(static value => (byte)value)];
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = CipherId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        cipher.Uris.Add(new LoginUri { Value = "https://added.test" });
        CipherResponse baseline = ExistingCipher(
            cipher,
            EncString.Encrypt(itemKey, userKey.Key),
            CreateRichLoginData(itemKey, "[]"u8.ToArray(), "[]"u8.ToArray(), unsupportedTotp: true, nullUris: true));

        CipherRequest request = CipherRequestFactory.BuildRequest(userKey, cipher, in baseline);

        using JsonDocument updated = JsonDocument.Parse(request.Data!);
        Assert.Equal("https://added.test", Decode(updated.RootElement.GetProperty("uris")[0].GetProperty("uri"), itemKey));
        CryptographicOperations.ZeroMemory(itemKey);
    }

    [Fact]
    public void Build_update_request_keeps_metadata_with_the_matching_type_when_duplicate_uris_reorder()
    {
        using var userKey = CreateUserKey();
        byte[] itemKey = [.. Enumerable.Range(65, 64).Select(static value => (byte)value)];
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = CipherId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        cipher.Uris.Add(new LoginUri { Value = "https://example.test", Match = LoginUri.MatchType.Exact });
        cipher.Uris.Add(new LoginUri { Value = "https://example.test", Match = LoginUri.MatchType.Domain });
        CipherResponse baseline = ExistingCipher(
            cipher,
            EncString.Encrypt(itemKey, userKey.Key),
            CreateRichLoginData(
                itemKey,
                "[]"u8.ToArray(),
                "[]"u8.ToArray(),
                unsupportedTotp: true,
                duplicateUriValues: true));

        CipherRequest request = CipherRequestFactory.BuildRequest(userKey, cipher, in baseline);

        using JsonDocument updated = JsonDocument.Parse(request.Data!);
        JsonElement uris = updated.RootElement.GetProperty("uris");
        Assert.Equal((int)LoginUri.MatchType.Exact, uris[0].GetProperty("match").GetInt32());
        Assert.Equal("keep-second-checksum", uris[0].GetProperty("checksum").GetString());
        Assert.Equal((int)LoginUri.MatchType.Domain, uris[1].GetProperty("match").GetInt32());
        Assert.Equal("keep-checksum", uris[1].GetProperty("checksum").GetString());
        CryptographicOperations.ZeroMemory(itemKey);
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
            Assert.Equal("folder-id", request.FolderId.Value);
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

    private static CipherResponse ExistingCipher(
        LoginCipher cipher,
        EncString protectedKey,
        byte[] data,
        bool archived = false,
        AttachmentDownloadResponse[]? attachments = null) => new()
    {
        Id = cipher.Id,
        ProtectedCipherKey = protectedKey,
        VaultCipherType = CipherType.Login,
        RevisionDate = cipher.RevisionDate,
        CreationDate = cipher.CreationDate,
        ArchivedDate = archived ? new DateTimeOffset(2025, 12, 1, 1, 2, 3, TimeSpan.Zero) : null,
        Favorite = cipher.Favorite,
        Reprompt = cipher.Reprompt,
        Edit = true,
        ViewPassword = true,
        Data = data,
        Attachments = attachments
    };

    private static byte[] CreateFields(ReadOnlySpan<byte> key)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartArray();
            writer.WriteStartObject();
            WriteEncrypted(writer, "name", "Linked field", key);
            WriteEncrypted(writer, "value", "Linked field value", key);
            writer.WriteNumber("type", 1);
            writer.WriteNumber("linkedId", 100);
            writer.WriteEndObject();
            writer.WriteEndArray();
        }

        return buffer.WrittenSpan.ToArray();
    }

    private static byte[] CreatePasswordHistory(ReadOnlySpan<byte> key)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WriteString("lastUsedDate", "2025-04-03T02:01:00Z");
            WriteEncrypted(writer, "password", "historical password", key);
            writer.WriteEndObject();
            writer.WriteEndArray();
        }

        return buffer.WrittenSpan.ToArray();
    }

    private static byte[] CreateRichLoginData(
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> fields,
        ReadOnlySpan<byte> history,
        bool unsupportedTotp,
        bool nullUris = false,
        bool duplicateUriValues = false)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            WriteEncrypted(writer, "name", "Old name", key);
            WriteEncrypted(writer, "notes", "Old notes", key);
            WriteEncrypted(writer, "username", "old-user", key);
            WriteEncrypted(writer, "password", "old-password", key);
            WriteEncrypted(writer, "totp", unsupportedTotp ? "unsupported format" : "JBSWY3DPEHPK3PXP", key);
            writer.WritePropertyName("fields");
            writer.WriteRawValue(fields);
            writer.WritePropertyName("passwordHistory");
            writer.WriteRawValue(history);
            writer.WritePropertyName("uris");
            if (nullUris)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStartArray();
                writer.WriteStartObject();
                WriteEncrypted(writer, "uri", "https://example.test", key);
                writer.WriteNumber("match", (int)LoginUri.MatchType.Domain);
                writer.WriteString("checksum", "keep-checksum");
                writer.WriteEndObject();
                writer.WriteStartObject();
                WriteEncrypted(writer, "uri", duplicateUriValues ? "https://example.test" : "https://second.test", key);
                writer.WriteNumber("match", (int)(duplicateUriValues ? LoginUri.MatchType.Exact : LoginUri.MatchType.Domain));
                writer.WriteString("checksum", "keep-second-checksum");
                writer.WriteEndObject();
                writer.WriteEndArray();
            }
            writer.WritePropertyName("fido2Credentials");
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WriteString("futureData", "first-future-credential-data");
            writer.WriteEndObject();
            writer.WriteStartObject();
            writer.WriteString("futureData", "second-future-credential-data");
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteStartObject("futureLoginProperty");
            writer.WriteString("opaque", "preserve-me");
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return buffer.WrittenSpan.ToArray();
    }

    private static void WriteEncrypted(Utf8JsonWriter writer, string name, string plaintext, ReadOnlySpan<byte> key)
    {
        writer.WritePropertyName(name);
        JsonSerializer.Serialize(writer, EncString.Encrypt(plaintext, key), CipherRequestFactoryTestJsonContext.Default.EncString);
    }

    private static string Decode(JsonElement element, ReadOnlySpan<byte> key)
        => JsonSerializer.Deserialize(element, CipherRequestFactoryTestJsonContext.Default.EncString).Decode(key);

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
            byte[] exactKey = cipherKey.AsSpan(..length).ToArray();
            CryptographicOperations.ZeroMemory(cipherKey);
            return exactKey;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(cipherKey);
            throw;
        }
    }
}

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(EncString))]
internal sealed partial class CipherRequestFactoryTestJsonContext : JsonSerializerContext;
