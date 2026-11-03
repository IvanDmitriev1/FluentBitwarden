using System.Security.Cryptography;
using BitwardenApi.Primitives;
using BitwardenApi.Vault.Ciphers;

using System.Buffers;
using System.Text.Json.Serialization;

using BitwardenApi.Vault.Attachments.Responses;
using BitwardenApi.Vault.Ciphers.Responses;

namespace BitwardenApi.Tests.Tests;

public sealed class CipherRequestBuilderTests
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

        CipherRequest request = CipherRequestBuilder.Build(userKey, cipher);

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
    public void Build_creation_request_rejects_an_existing_cipher_id()
    {
        using var userKey = new UnlockedUserKey(
            UserId.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            [.. Enumerable.Range(0, 64).Select(static value => (byte)value)]);
        var cipher = (SecureNoteCipher)Cipher.CreateBlankCipher(CipherType.SecureNote);
        cipher.Id = CipherId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        cipher.Reprompt = true;

        Assert.Throws<InvalidOperationException>(() => CipherRequestBuilder.Build(userKey, cipher));
    }

    [Fact]
    public void Build_update_request_keeps_the_original_key_and_typed_encrypted_data()
    {
        using var userKey = CreateUserKey();
        byte[] itemKey = [.. Enumerable.Range(65, 64).Select(static value => (byte)value)];
        EncString protectedKey = EncString.Encrypt(itemKey, userKey.Key);
        byte[] fields = CreateFields(itemKey);
        byte[] history = CreatePasswordHistory(itemKey);
        byte[] data = CreateRichLoginData(itemKey, fields, history, unsupportedTotp: true, includeFido: true, includeLegacyUri: true);
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = CipherId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        cipher.RevisionDate = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        cipher.Name = "Updated name";
        cipher.Notes = "Updated notes";
        cipher.Username = "updated-user";
        cipher.Password = "updated-password";
        cipher.Uris.Add(new LoginUri { Value = "https://example.test", Match = LoginUri.MatchType.Domain });
        cipher.Uris.Add(new LoginUri { Value = "https://second.test", Match = LoginUri.MatchType.Domain });
        cipher.Fido2Credential = FirstCredential();
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

        CipherRequest request = CipherUpdateRequestBuilder.Build(userKey, cipher, in baseline);
        byte[] requestKey = UnwrapKey(request, userKey);
        try
        {
            Assert.Equal(protectedKey, request.Key);
            Assert.Equal(cipher.RevisionDate, request.LastKnownRevisionDate);
            Assert.Equal(baseline.ArchivedDate, request.ArchivedDate);
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

            Assert.Equal("Updated notes", request.Notes.Decode(requestKey));
            CipherLoginRequest updated = Assert.IsType<CipherLoginRequest>(request.Login);
            Assert.Equal("updated-user", updated.Username.Decode(requestKey));
            Assert.Equal("updated-password", updated.Password.Decode(requestKey));
            Assert.Equal("unsupported format", updated.Totp.Decode(requestKey));
            Assert.Equal(1, Assert.Single(request.Fields!).Type);
            Assert.Equal("Linked field", request.Fields![0].Name.Decode(requestKey));
            Assert.Equal("Linked field value", request.Fields[0].Value.Decode(requestKey));
            Assert.Equal(100, request.Fields[0].LinkedId);
            Assert.Equal("historical password", Assert.Single(request.PasswordHistory!).Password.Decode(requestKey));
            Assert.Equal(new DateTimeOffset(2025, 4, 3, 2, 1, 0, TimeSpan.Zero), request.PasswordHistory![0].LastUsedDate);
            Assert.Equal(2, updated.Fido2Credentials.Count);
            Assert.Equal("12345678-1234-5678-9abc-def012345678", updated.Fido2Credentials[0].CredentialId.Decode(requestKey));
            Assert.Equal("12345678-1234-5678-9abc-def012345679", updated.Fido2Credentials[1].CredentialId.Decode(requestKey));
            Assert.Equal("https://example.test", updated.Uris[0].Uri.Decode(requestKey));
            Assert.Equal("keep-checksum", updated.Uris[0].UriChecksum);
            Assert.Equal("https://second.test", updated.Uris[1].Uri.Decode(requestKey));
            Assert.Equal("keep-second-checksum", updated.Uris[1].UriChecksum);
            using JsonDocument serialized = JsonDocument.Parse(JsonSerializer.Serialize(request, CipherRequestBuilderTestJsonContext.Default.CipherRequest));
            Assert.False(serialized.RootElement.TryGetProperty("data", out _));
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
        byte[] data = CreateRichLoginData(userKey.Key, "[]"u8.ToArray(), "[]"u8.ToArray(), unsupportedTotp: false, nullUris: true);
        CipherResponse baseline = ExistingCipher(cipher, EncString.Empty, data);

        CipherRequest request = CipherUpdateRequestBuilder.Build(userKey, cipher, in baseline);
        Assert.True(request.Key.IsEmpty);
        Assert.Equal("Legacy edit", request.Name.Decode(userKey.Key));
        Assert.True(Assert.IsType<CipherLoginRequest>(request.Login).Totp.IsEmpty);
    }

    [Fact]
    public void Build_update_request_reencrypts_unchanged_editable_scalars_for_every_type()
    {
        using var userKey = CreateUserKey();
        byte[] itemKey = [.. Enumerable.Range(65, 64).Select(static value => (byte)value)];
        try
        {
            foreach (CipherType type in Enum.GetValues<CipherType>())
            {
                Cipher cipher = Cipher.CreateBlankCipher(type);
                cipher.Id = CipherId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
                cipher.RevisionDate = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
                var originalValues = new Dictionary<string, EncString>(StringComparer.Ordinal);
                byte[] data = CreateTypedCipherData(type, itemKey, originalValues);
                SetTypedCipherValues(cipher);
                CipherResponse original = new()
                {
                    Id = cipher.Id,
                    ProtectedCipherKey = EncString.Encrypt(itemKey, userKey.Key),
                    VaultCipherType = type,
                    RevisionDate = cipher.RevisionDate,
                    CreationDate = cipher.CreationDate,
                    Favorite = cipher.Favorite,
                    Reprompt = cipher.Reprompt,
                    Edit = true,
                    ViewPassword = true,
                    Data = data
                };

                CipherRequest request = CipherUpdateRequestBuilder.Build(userKey, cipher, in original);

                Assert.Equal(type, request.Type);
                Assert.NotEqual(originalValues["name"], request.Name);
                Assert.NotEqual(originalValues["notes"], request.Notes);
                Assert.Equal("same name", request.Name.Decode(itemKey));
                Assert.Equal("same notes", request.Notes.Decode(itemKey));
                switch (request)
                {
                    case { Login: { } login }:
                        Assert.NotEqual(originalValues["username"], login.Username);
                        Assert.NotEqual(originalValues["password"], login.Password);
                        Assert.Equal("same username", login.Username.Decode(itemKey));
                        Assert.Equal("same password", login.Password.Decode(itemKey));
                        break;
                    case { Card: { } card }:
                        Assert.NotEqual(originalValues["cardholderName"], card.CardholderName);
                        Assert.NotEqual(originalValues["brand"], card.Brand);
                        Assert.NotEqual(originalValues["number"], card.Number);
                        Assert.NotEqual(originalValues["expMonth"], card.ExpMonth);
                        Assert.NotEqual(originalValues["expYear"], card.ExpYear);
                        Assert.NotEqual(originalValues["code"], card.Code);
                        Assert.Equal("same cardholder", card.CardholderName.Decode(itemKey));
                        Assert.Equal("same brand", card.Brand.Decode(itemKey));
                        Assert.Equal("same number", card.Number.Decode(itemKey));
                        Assert.Equal("same month", card.ExpMonth.Decode(itemKey));
                        Assert.Equal("same year", card.ExpYear.Decode(itemKey));
                        Assert.Equal("same code", card.Code.Decode(itemKey));
                        break;
                    case { Identity: { } identity }:
                        Assert.NotEqual(originalValues["firstName"], identity.FirstName);
                        Assert.NotEqual(originalValues["email"], identity.Email);
                        Assert.Equal("same first name", identity.FirstName.Decode(itemKey));
                        Assert.Equal("same email", identity.Email.Decode(itemKey));
                        break;
                    case { SecureNote: { Type: 7 } }:
                        break;
                    case { SshKey: { } sshKey }:
                        Assert.NotEqual(originalValues["privateKey"], sshKey.PrivateKey);
                        Assert.NotEqual(originalValues["publicKey"], sshKey.PublicKey);
                        Assert.NotEqual(originalValues["keyFingerprint"], sshKey.KeyFingerprint);
                        Assert.Equal("same private key", sshKey.PrivateKey.Decode(itemKey));
                        Assert.Equal("same public key", sshKey.PublicKey.Decode(itemKey));
                        Assert.Equal("same fingerprint", sshKey.KeyFingerprint.Decode(itemKey));
                        break;
                    default:
                        throw new Xunit.Sdk.XunitException($"No update request was created for {type}.");
                }
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(itemKey);
        }
    }

    [Fact]
    public void Build_update_request_rejects_malformed_original_data()
    {
        using var userKey = CreateUserKey();
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = CipherId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        CipherResponse baseline = ExistingCipher(cipher, EncString.Empty, "not-json"u8.ToArray());

        Assert.ThrowsAny<JsonException>(() => CipherUpdateRequestBuilder.Build(userKey, cipher, in baseline));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Build_update_request_rejects_missing_required_history_or_passkey_timestamps(bool passkey)
    {
        using var userKey = CreateUserKey();
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = CipherId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        CipherResponse baseline = ExistingCipher(cipher, EncString.Empty, CreateDataMissingTimestamp(userKey.Key, passkey));

        Assert.ThrowsAny<JsonException>(() => CipherUpdateRequestBuilder.Build(userKey, cipher, in baseline));
    }

    [Theory]
    [InlineData("common")]
    [InlineData("field")]
    [InlineData("uri")]
    [InlineData("credential")]
    public void Build_update_request_rejects_unknown_members_at_every_payload_depth(string location)
    {
        using var userKey = CreateUserKey();
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = CipherId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        CipherResponse baseline = ExistingCipher(cipher, EncString.Empty, CreateDataWithUnknownMember(userKey.Key, location));

        Assert.ThrowsAny<JsonException>(() => CipherUpdateRequestBuilder.Build(userKey, cipher, in baseline));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Build_update_request_rejects_removing_an_unprojectable_single_passkey(bool missingOneValue)
    {
        using var userKey = CreateUserKey();
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = CipherId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        byte[] data = CreateMalformedSingleCredentialData(userKey.Key, missingOneValue);
        CipherResponse baseline = ExistingCipher(cipher, EncString.Empty, data);

        Assert.ThrowsAny<JsonException>(() => CipherUpdateRequestBuilder.Build(userKey, cipher, in baseline));
    }

    [Fact]
    public void Build_update_request_preserves_all_passkeys_when_first_projection_matches_noncanonical_wire_values()
    {
        using var userKey = CreateUserKey();
        byte[] itemKey = [.. Enumerable.Range(65, 64).Select(static value => (byte)value)];
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = CipherId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        cipher.Fido2Credential = FirstCredential(discoverable: true);
        byte[] data = CreateRichLoginData(
            itemKey,
            "[]"u8.ToArray(),
            "[]"u8.ToArray(),
            unsupportedTotp: true,
            nullUris: true,
            includeFido: true,
            nonCanonicalFirstFido: true);
        CipherResponse baseline = ExistingCipher(cipher, EncString.Encrypt(itemKey, userKey.Key), data);

        CipherRequest request = CipherUpdateRequestBuilder.Build(userKey, cipher, in baseline);
        EncryptedFido2Credential preserved = Assert.IsType<CipherLoginRequest>(request.Login).Fido2Credentials[0];
        byte[] requestKey = UnwrapKey(request, userKey);
        try
        {
            Assert.Equal(2, Assert.IsType<CipherLoginRequest>(request.Login).Fido2Credentials.Count);
            Assert.Equal("{12345678-1234-5678-9ABC-DEF012345678}", preserved.CredentialId.Decode(requestKey));
            Assert.Equal("PUBLIC-KEY", preserved.KeyType.Decode(requestKey));
            Assert.Equal("0000", preserved.Counter.Decode(requestKey));
            Assert.Equal("1", preserved.Discoverable.Decode(requestKey));
            Assert.Equal(new DateTimeOffset(2026, 2, 3, 4, 5, 6, TimeSpan.Zero), preserved.CreationDate);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(requestKey);
            CryptographicOperations.ZeroMemory(itemKey);
        }
    }

    [Fact]
    public void Fido2_content_comparison_uses_array_contents_and_scalar_fields()
    {
        Fido2Credential original = FirstCredential();
        Fido2Credential sameContent = new()
        {
            CredentialId = original.CredentialId.ToArray(),
            KeyType = original.KeyType,
            KeyAlgorithm = original.KeyAlgorithm,
            KeyCurve = original.KeyCurve,
            KeyValue = original.KeyValue.ToArray(),
            RpId = original.RpId,
            RpName = original.RpName,
            UserHandle = original.UserHandle.ToArray(),
            UserName = original.UserName,
            UserDisplayName = original.UserDisplayName,
            Counter = original.Counter,
            Discoverable = original.Discoverable,
            CreationDate = original.CreationDate
        };
        Fido2Credential changedBytes = new()
        {
            CredentialId = original.CredentialId.ToArray(),
            KeyType = original.KeyType,
            KeyAlgorithm = original.KeyAlgorithm,
            KeyCurve = original.KeyCurve,
            KeyValue = [1, 2, 4],
            RpId = original.RpId,
            RpName = original.RpName,
            UserHandle = original.UserHandle.ToArray(),
            UserName = original.UserName,
            UserDisplayName = original.UserDisplayName,
            Counter = original.Counter,
            Discoverable = original.Discoverable,
            CreationDate = original.CreationDate
        };
        Fido2Credential changedScalar = new()
        {
            CredentialId = original.CredentialId.ToArray(),
            KeyType = original.KeyType,
            KeyAlgorithm = original.KeyAlgorithm,
            KeyCurve = original.KeyCurve,
            KeyValue = original.KeyValue.ToArray(),
            RpId = original.RpId,
            RpName = original.RpName,
            UserHandle = original.UserHandle.ToArray(),
            UserName = original.UserName,
            UserDisplayName = original.UserDisplayName,
            Counter = original.Counter + 1,
            Discoverable = original.Discoverable,
            CreationDate = original.CreationDate
        };

        Assert.True(original.HasSameContentAs(sameContent));
        Assert.False(original.HasSameContentAs(changedBytes));
        Assert.False(original.HasSameContentAs(changedScalar));
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

        Assert.Throws<InvalidOperationException>(() => CipherUpdateRequestBuilder.Build(userKey, cipher, in baseline));
        CryptographicOperations.ZeroMemory(itemKey);
    }

    [Fact]
    public void Build_update_request_allows_adding_a_uri_when_the_baseline_has_no_uri_list()
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

        CipherRequest request = CipherUpdateRequestBuilder.Build(userKey, cipher, in baseline);

        Assert.Equal("https://added.test", Assert.Single(Assert.IsType<CipherLoginRequest>(request.Login).Uris).Uri.Decode(itemKey));
        CryptographicOperations.ZeroMemory(itemKey);
    }

    [Fact]
    public void Build_update_request_rejects_uri_reorder_when_records_have_checksums()
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

        Assert.Throws<InvalidOperationException>(() => CipherUpdateRequestBuilder.Build(userKey, cipher, in baseline));
        CryptographicOperations.ZeroMemory(itemKey);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Build_update_request_preserves_null_uri_match_only_when_the_projection_is_unchanged(bool editUri)
    {
        using var userKey = CreateUserKey();
        var cipher = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        cipher.Id = CipherId.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        cipher.Uris.Add(new LoginUri { Value = "https://example.test", Match = LoginUri.MatchType.Domain });
        if (editUri)
            cipher.Uris[0].Value = "https://changed.test";

        CipherResponse baseline = ExistingCipher(cipher, EncString.Empty, CreateNullMatchUriData(userKey.Key));

        if (editUri)
        {
            Assert.Throws<InvalidOperationException>(() => CipherUpdateRequestBuilder.Build(userKey, cipher, in baseline));
            return;
        }

        CipherRequest request = CipherUpdateRequestBuilder.Build(userKey, cipher, in baseline);

        Assert.Null(Assert.Single(Assert.IsType<CipherLoginRequest>(request.Login).Uris).Match);
        Assert.Equal("https://example.test", Assert.Single(Assert.IsType<CipherLoginRequest>(request.Login).Uris).Uri.Decode(userKey.Key));
    }

    [Fact]
    public void Build_card_request_encrypts_each_card_field_and_preserves_folder_and_revision()
    {
        using var userKey = CreateUserKey();
        var cipher = (CardCipher)Cipher.CreateBlankCipher(CipherType.Card);
        cipher.FolderId = FolderId.Parse("folder-id");
        cipher.RevisionDate = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(3));
        cipher.Favorite = true;
        cipher.CardholderName = "Alice Example";
        cipher.Brand = "VISA";
        cipher.Number = "4111111111111111";
        cipher.ExpMonth = "03";
        cipher.ExpYear = "2030";
        cipher.Code = "123";

        CipherRequest request = CipherRequestBuilder.Build(userKey, cipher);
        byte[] cipherKey = UnwrapKey(request, userKey);
        try
        {
            Assert.Equal(CipherType.Card, request.Type);
            Assert.True(request.Favorite);
            Assert.Equal("folder-id", request.FolderId.Value);
            Assert.Null(request.LastKnownRevisionDate);
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

        CipherRequest request = CipherRequestBuilder.Build(userKey, cipher);
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

        CipherRequest request = CipherRequestBuilder.Build(userKey, cipher);
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

        CipherRequest request = CipherRequestBuilder.Build(userKey, cipher);
        byte[] cipherKey = UnwrapKey(request, userKey);
        try
        {
            CipherLoginRequest login = Assert.IsType<CipherLoginRequest>(request.Login);
            Assert.Equal(
                "otpauth://totp/?secret=GEZDGNBVGY3TQOJQ&algorithm=SHA256&digits=8&period=45",
                login.Totp.Decode(cipherKey));
            EncryptedFido2Credential credential = Assert.Single(login.Fido2Credentials);
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
            Assert.Equal(creationDate, credential.CreationDate);
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

    private static byte[] CreateDataMissingTimestamp(ReadOnlySpan<byte> key, bool passkey)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            WriteEncrypted(writer, "name", "Old name", key);
            if (passkey)
            {
                writer.WriteStartArray("fido2Credentials");
                WriteFido2Credential(writer, key, "12345678-1234-5678-9abc-def012345678", omitCreationDate: true);
                writer.WriteEndArray();
            }
            else
            {
                writer.WriteStartArray("passwordHistory");
                writer.WriteStartObject();
                WriteEncrypted(writer, "password", "historical password", key);
                writer.WriteEndObject();
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        return buffer.WrittenSpan.ToArray();
    }

    private static byte[] CreateNullMatchUriData(ReadOnlySpan<byte> key)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            WriteEncrypted(writer, "name", "Existing name", key);
            writer.WriteStartArray("uris");
            writer.WriteStartObject();
            WriteEncrypted(writer, "uri", "https://example.test", key);
            writer.WriteNull("match");
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return buffer.WrittenSpan.ToArray();
    }

    private static byte[] CreateTypedCipherData(CipherType type, byte[] key, Dictionary<string, EncString> encryptedValues)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            Write("name", "same name");
            Write("notes", "same notes");
            switch (type)
            {
                case CipherType.Login:
                    Write("username", "same username");
                    Write("password", "same password");
                    break;
                case CipherType.SecureNote:
                    writer.WriteNumber("type", 7);
                    break;
                case CipherType.Card:
                    Write("cardholderName", "same cardholder");
                    Write("brand", "same brand");
                    Write("number", "same number");
                    Write("expMonth", "same month");
                    Write("expYear", "same year");
                    Write("code", "same code");
                    break;
                case CipherType.Identity:
                    Write("firstName", "same first name");
                    Write("email", "same email");
                    break;
                case CipherType.SshKey:
                    Write("privateKey", "same private key");
                    Write("publicKey", "same public key");
                    Write("keyFingerprint", "same fingerprint");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported cipher type.");
            }

            writer.WriteEndObject();

            void Write(string property, string value)
            {
                EncString encrypted = EncString.Encrypt(value, key);
                encryptedValues.Add(property, encrypted);
                writer.WritePropertyName(property);
                JsonSerializer.Serialize(writer, encrypted, CipherRequestBuilderTestJsonContext.Default.EncString);
            }
        }

        return buffer.WrittenSpan.ToArray();
    }

    private static void SetTypedCipherValues(Cipher cipher)
    {
        cipher.Name = "same name";
        cipher.Notes = "same notes";
        switch (cipher)
        {
            case LoginCipher login:
                login.Username = "same username";
                login.Password = "same password";
                break;
            case CardCipher card:
                card.CardholderName = "same cardholder";
                card.Brand = "same brand";
                card.Number = "same number";
                card.ExpMonth = "same month";
                card.ExpYear = "same year";
                card.Code = "same code";
                break;
            case IdentityCipher identity:
                identity.FirstName = "same first name";
                identity.Email = "same email";
                break;
            case SecureNoteCipher:
                break;
            case SshKeyCipher sshKey:
                sshKey.PrivateKey = "same private key";
                sshKey.PublicKey = OpenSshPublicKey.CreateUnparsed("same public key");
                sshKey.KeyFingerprint = "same fingerprint";
                break;
        }
    }

    private static byte[] CreateRichLoginData(
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> fields,
        ReadOnlySpan<byte> history,
        bool unsupportedTotp,
        bool nullUris = false,
        bool duplicateUriValues = false,
        bool includeFido = false,
        bool includeLegacyUri = false,
        bool nonCanonicalFirstFido = false)
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
            if (includeLegacyUri)
                WriteEncrypted(writer, "uri", "https://example.test", key);
            writer.WritePropertyName("fields");
            writer.WriteRawValue(fields);
            writer.WritePropertyName("passwordHistory");
            writer.WriteRawValue(history);
            if (!nullUris)
            {
                writer.WritePropertyName("uris");
                writer.WriteStartArray();
                writer.WriteStartObject();
                WriteEncrypted(writer, "uri", "https://example.test", key);
                writer.WriteNumber("match", (int)LoginUri.MatchType.Domain);
                writer.WriteString("uriChecksum", "keep-checksum");
                writer.WriteEndObject();
                writer.WriteStartObject();
                WriteEncrypted(writer, "uri", duplicateUriValues ? "https://example.test" : "https://second.test", key);
                writer.WriteNumber("match", (int)(duplicateUriValues ? LoginUri.MatchType.Exact : LoginUri.MatchType.Domain));
                writer.WriteString("uriChecksum", "keep-second-checksum");
                writer.WriteEndObject();
                writer.WriteEndArray();
            }
            writer.WritePropertyName("fido2Credentials");
            writer.WriteStartArray();
            if (includeFido)
            {
                WriteFido2Credential(writer, key, "12345678-1234-5678-9abc-def012345678", nonCanonicalFirstFido);
                WriteFido2Credential(writer, key, "12345678-1234-5678-9abc-def012345679");
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return buffer.WrittenSpan.ToArray();
    }

    private static void WriteEncrypted(Utf8JsonWriter writer, string name, string plaintext, ReadOnlySpan<byte> key)
    {
        writer.WritePropertyName(name);
        JsonSerializer.Serialize(writer, EncString.Encrypt(plaintext, key), CipherRequestBuilderTestJsonContext.Default.EncString);
    }

    private static void WriteFido2Credential(
        Utf8JsonWriter writer,
        ReadOnlySpan<byte> key,
        string credentialId,
        bool nonCanonical = false,
        bool unknownMember = false,
        bool omitRpName = false,
        bool omitCreationDate = false)
    {
        writer.WriteStartObject();
        WriteEncrypted(writer, "credentialId", nonCanonical ? "{" + credentialId.ToUpperInvariant() + "}" : credentialId, key);
        WriteEncrypted(writer, "keyType", nonCanonical ? "PUBLIC-KEY" : "public-key", key);
        WriteEncrypted(writer, "keyAlgorithm", nonCanonical ? "ecdsa" : "ECDSA", key);
        WriteEncrypted(writer, "keyCurve", nonCanonical ? "p-256" : "P-256", key);
        WriteEncrypted(writer, "keyValue", "AQID", key);
        WriteEncrypted(writer, "rpId", "example.test", key);
        if (!omitRpName)
        {
            WriteEncrypted(writer, "rpName", "Example", key);
        }
        WriteEncrypted(writer, "userHandle", "AQID", key);
        WriteEncrypted(writer, "userName", "alice", key);
        WriteEncrypted(writer, "userDisplayName", "Alice Example", key);
        WriteEncrypted(writer, "counter", nonCanonical ? "0000" : "0", key);
        WriteEncrypted(writer, "discoverable", nonCanonical ? "1" : "false", key);
        if (!omitCreationDate)
        {
            writer.WriteString("creationDate", nonCanonical ? "2026-02-03T06:05:06+02:00" : "2026-02-03T04:05:06Z");
        }
        if (unknownMember)
        {
            writer.WriteBoolean("futureCredential", true);
        }
        writer.WriteEndObject();
    }

    private static Fido2Credential FirstCredential(bool discoverable = false) => new()
    {
        CredentialId = Guid.Parse("12345678-1234-5678-9abc-def012345678").ToByteArray(bigEndian: true),
        KeyType = Fido2CredentialKeyType.PublicKey,
        KeyAlgorithm = Fido2CredentialKeyAlgorithm.Ecdsa,
        KeyCurve = Fido2CredentialKeyCurve.P256,
        KeyValue = [1, 2, 3],
        RpId = "example.test",
        RpName = "Example",
        UserHandle = [1, 2, 3],
        UserName = "alice",
        UserDisplayName = "Alice Example",
        Counter = 0,
        Discoverable = discoverable,
        CreationDate = new DateTimeOffset(2026, 2, 3, 4, 5, 6, TimeSpan.Zero)
    };

    private static byte[] CreateDataWithUnknownMember(ReadOnlySpan<byte> key, string location)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            WriteEncrypted(writer, "name", "Old name", key);
            switch (location)
            {
                case "common":
                    writer.WriteBoolean("futureProperty", true);
                    break;
                case "field":
                    writer.WriteStartArray("fields");
                    writer.WriteStartObject();
                    WriteEncrypted(writer, "name", "Field", key);
                    WriteEncrypted(writer, "value", "Value", key);
                    writer.WriteNumber("type", 0);
                    writer.WriteBoolean("futureField", true);
                    writer.WriteEndObject();
                    writer.WriteEndArray();
                    break;
                case "uri":
                    writer.WriteStartArray("uris");
                    writer.WriteStartObject();
                    WriteEncrypted(writer, "uri", "https://example.test", key);
                    writer.WriteNumber("match", 0);
                    writer.WriteBoolean("futureUri", true);
                    writer.WriteEndObject();
                    writer.WriteEndArray();
                    break;
                case "credential":
                    writer.WriteStartArray("fido2Credentials");
                    WriteFido2Credential(writer, key, "12345678-1234-5678-9abc-def012345678", unknownMember: true);
                    writer.WriteEndArray();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(location), location, "Unknown fixture member location.");
            }

            writer.WriteEndObject();
        }

        return buffer.WrittenSpan.ToArray();
    }

    private static byte[] CreateMalformedSingleCredentialData(ReadOnlySpan<byte> key, bool missingOneValue)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            WriteEncrypted(writer, "name", "Old name", key);
            writer.WriteStartArray("fido2Credentials");
            if (missingOneValue)
            {
                WriteFido2Credential(writer, key, "12345678-1234-5678-9abc-def012345678", omitRpName: true);
            }
            else
            {
                writer.WriteStartObject();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return buffer.WrittenSpan.ToArray();
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
[JsonSerializable(typeof(CipherRequest))]
internal sealed partial class CipherRequestBuilderTestJsonContext : JsonSerializerContext;
