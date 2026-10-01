using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using BitwardenApi.Vault.Attachments.Contracts;
using BitwardenApi.Vault.Cryptography;
using FluentBitwarden.AppHost.Modules.Vault.Persistence.Serialization;
using static FluentBitwarden.AppHost.IntegrationTests.Modules.Vault.VaultParserTestData;

namespace FluentBitwarden.AppHost.IntegrationTests.Modules.Vault;

public sealed partial class VaultDataParserTests
{
    private readonly ITestOutputHelper _output;

    public VaultDataParserTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void Parses_encrypted_login_fields_using_the_base_key()
    {
        byte[] keyBytes = Enumerable.Range(0, 64).Select(static value => (byte)value).ToArray();
        using var baseKey = new TestKey(keyBytes);
        byte[] payload = CreateLoginPayload(keyBytes);
        DateTimeOffset revisionDate = DateTimeOffset.Parse("2026-09-30T12:00:00Z");
        DateTimeOffset creationDate = DateTimeOffset.Parse("2026-09-29T12:00:00Z");
        var dto = new VaultCipherResponse
        {
            Id = CipherId.Parse("synthetic-cipher"),
            FolderId = FolderId.Parse("synthetic-folder"),
            VaultCipherType = VaultCipherType.Login,
            RevisionDate = revisionDate,
            CreationDate = creationDate,
            Favorite = true,
            Reprompt = false,
            Edit = true,
            ViewPassword = false,
            Data = payload
        };

        var parsed = Assert.IsType<LoginVaultCipher>(ParseCipher(in dto, payload, baseKey));

        Assert.Equal("Synthetic item", parsed.Name);
        Assert.Equal("synthetic-user", parsed.Username);
        Assert.Equal("synthetic-password", parsed.Password);
        Assert.Equal(dto.Id, parsed.Id);
        Assert.Equal(dto.FolderId, parsed.FolderId);
        Assert.Equal(dto.Favorite, parsed.Favorite);
        Assert.Equal(dto.Reprompt, parsed.Reprompt);
        Assert.Equal(revisionDate, parsed.RevisionDate);
        Assert.Equal(creationDate, parsed.CreationDate);
    }

    [Fact]
    public void Payload_fields_do_not_override_database_metadata()
    {
        byte[] keyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        using var baseKey = new TestKey(keyBytes);
        byte[] payload = WritePayload(keyBytes, static (writer, key) =>
        {
            writer.WriteStartObject();
            WriteEncrypted(writer, "Name", "Synthetic item", key);
            WriteEncrypted(writer, "Username", "synthetic-user", key);
            WriteEncrypted(writer, "Password", "synthetic-password", key);
            writer.WriteString("Id", "json-override");
            writer.WriteBoolean("favorite", false);
            writer.WriteString("creationDate", "1900-01-01T00:00:00Z");
            writer.WriteString("deletedDate", "1900-01-01T00:00:00Z");
            writer.WriteString("folderId", "json-folder-override");
            writer.WriteString("type", "secureNote");
            writer.WriteStartObject("attachments");
            writer.WriteString("size", "not a file size");
            writer.WriteEndObject();
            writer.WriteEndObject();
        });
        var dto = CreateDto(VaultCipherType.Login, payload);

        var parsed = Assert.IsType<LoginVaultCipher>(
            ParseCipher(in dto, payload, baseKey));

        Assert.Equal("Synthetic item", parsed.Name);
        Assert.Equal("synthetic-user", parsed.Username);
        Assert.Equal("synthetic-password", parsed.Password);
        Assert.Equal(dto.Id, parsed.Id);
        Assert.True(parsed.Favorite);
        Assert.Equal(dto.CreationDate, parsed.CreationDate);
    }

    [Fact]
    public void Missing_login_fields_keep_their_existing_defaults()
    {
        byte[] keyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        using var baseKey = new TestKey(keyBytes);
        byte[] payload = "{}"u8.ToArray();
        var dto = CreateDto(VaultCipherType.Login, payload);

        var parsed = Assert.IsType<LoginVaultCipher>(
            ParseCipher(in dto, payload, baseKey));

        Assert.Equal(string.Empty, parsed.Name);
        Assert.Null(parsed.Username);
        Assert.Null(parsed.Password);
    }

    [Fact]
    public void Explicit_null_login_credentials_are_rejected()
    {
        byte[] keyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        using var baseKey = new TestKey(keyBytes);
        foreach (string requiredProperty in new[] { "username", "password" })
        {
            byte[] payload = WritePayload(keyBytes, (writer, key) =>
            {
                writer.WriteStartObject();
                WriteEncrypted(writer, "name", "Synthetic item", key);
                writer.WriteNull(requiredProperty);
                writer.WriteEndObject();
            });
            var dto = CreateDto(VaultCipherType.Login, payload);

            Assert.Throws<JsonException>(() => ParseCipher(in dto, payload, baseKey));
        }
    }

    [Fact]
    public void Optional_notes_card_and_identity_values_accept_explicit_null()
    {
        byte[] keyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        using var baseKey = new TestKey(keyBytes);
        byte[] notePayload = "{\"notes\":null}"u8.ToArray();
        byte[] cardPayload = "{\"cardholderName\":null,\"number\":null}"u8.ToArray();
        byte[] identityPayload = "{\"firstName\":null,\"phone\":null}"u8.ToArray();
        var noteDto = CreateDto(VaultCipherType.SecureNote, notePayload);
        var cardDto = CreateDto(VaultCipherType.Card, cardPayload);
        var identityDto = CreateDto(VaultCipherType.Identity, identityPayload);

        var note = Assert.IsType<SecureNoteVaultCipher>(ParseCipher(in noteDto, notePayload, baseKey));
        var card = Assert.IsType<CardVaultCipher>(ParseCipher(in cardDto, cardPayload, baseKey));
        var identity = Assert.IsType<IdentityVaultCipher>(ParseCipher(in identityDto, identityPayload, baseKey));

        Assert.Null(note.Notes);
        Assert.Null(card.CardholderName);
        Assert.Null(card.Number);
        Assert.Null(identity.FirstName);
        Assert.Null(identity.Phone);
    }

    [Fact]
    public void Null_and_invalid_totp_values_keep_existing_null_fallback()
    {
        byte[] keyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        using var baseKey = new TestKey(keyBytes);
        byte[] nullPayload = "{\"totp\":null}"u8.ToArray();
        byte[] invalidPayload = WritePayload(keyBytes, static (writer, key) =>
        {
            writer.WriteStartObject();
            WriteEncrypted(writer, "totp", "%%%not-base32%%%", key);
            writer.WriteEndObject();
        });

        foreach (byte[] payload in new[] { nullPayload, invalidPayload })
        {
            var dto = CreateDto(VaultCipherType.Login, payload);
            var login = Assert.IsType<LoginVaultCipher>(ParseCipher(in dto, payload, baseKey));
            Assert.Null(login.Totp);
        }
    }

    [Fact]
    public void Records_warmed_allocations_for_small_login_and_long_secure_note()
    {
        byte[] keyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        using var baseKey = new TestKey(keyBytes);
        long setupStart = GC.GetAllocatedBytesForCurrentThread();
        var parser = new FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing.VaultDataParser();
        long setupBytes = GC.GetAllocatedBytesForCurrentThread() - setupStart;

        byte[] loginPayload = CreateLoginPayload(keyBytes);
        var loginDto = CreateDto(VaultCipherType.Login, loginPayload);
        long loginBytes = MeasureAllocatedBytes(parser, in loginDto, loginPayload, baseKey);

        byte[] complexPayload = CreateLoginPayloadWithUriAndFido2(keyBytes);
        var complexDto = CreateDto(VaultCipherType.Login, complexPayload);
        long complexBytes = MeasureAllocatedBytes(parser, in complexDto, complexPayload, baseKey);

        byte[] notePayload = WritePayload(keyBytes, static (writer, key) =>
        {
            writer.WriteStartObject();
            WriteEncrypted(writer, "name", "Long synthetic note", key);
            WriteEncrypted(writer, "notes", new string('x', 4096), key);
            writer.WriteEndObject();
        });
        var noteDto = CreateDto(VaultCipherType.SecureNote, notePayload);
        long noteBytes = MeasureAllocatedBytes(parser, in noteDto, notePayload, baseKey);

        _output.WriteLine($"Parser instance setup allocation (source-generated resolver already initialized): {setupBytes} B.");
        _output.WriteLine($"Warmed parser allocation per parse: small login={loginBytes} B; login with URI/FIDO2={complexBytes} B; long secure note={noteBytes} B.");
        Assert.True(loginBytes > 0);
        Assert.True(complexBytes > 0);
        Assert.True(noteBytes > 0);
    }

    [Fact]
    public void Accepts_camel_case_and_pascal_case_properties()
    {
        byte[] keyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        using var baseKey = new TestKey(keyBytes);
        byte[] payload = WritePayload(keyBytes, static (writer, key) =>
        {
            writer.WriteStartObject();
            WriteEncrypted(writer, "Name", "Pascal item", key);
            WriteEncrypted(writer, "Username", "Pascal user", key);
            writer.WriteEndObject();
        });
        var dto = CreateDto(VaultCipherType.Login, payload);

        var parsed = Assert.IsType<LoginVaultCipher>(
            ParseCipher(in dto, payload, baseKey));

        Assert.Equal("Pascal item", parsed.Name);
        Assert.Equal("Pascal user", parsed.Username);
    }

    [Fact]
    public void Preserves_totp_parsing_and_parses_ssh_public_keys()
    {
        byte[] keyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        using var baseKey = new TestKey(keyBytes);
        byte[] loginPayload = WritePayload(keyBytes, static (writer, key) =>
        {
            writer.WriteStartObject();
            WriteEncrypted(writer, "name", "TOTP item", key);
            WriteEncrypted(writer, "totp", "JBSWY3DPEHPK3PXP", key);
            writer.WriteEndObject();
        });
        var loginDto = CreateDto(VaultCipherType.Login, loginPayload);
        var login = Assert.IsType<LoginVaultCipher>(
            ParseCipher(in loginDto, loginPayload, baseKey));

        byte[] sshPayload = WritePayload(keyBytes, static (writer, key) =>
        {
            writer.WriteStartObject();
            WriteEncrypted(writer, "name", "SSH item", key);
            WriteEncrypted(writer, "publicKey", "ssh-ed25519 AQID", key);
            writer.WriteEndObject();
        });
        var sshDto = CreateDto(VaultCipherType.SshKey, sshPayload);
        var ssh = Assert.IsType<SshKeyVaultCipher>(
            ParseCipher(in sshDto, sshPayload, baseKey));

        Assert.NotNull(login.Totp);
        Assert.Equal("ssh-ed25519 AQID", ssh.PublicKey.RawKey);
        Assert.Equal(new byte[] { 1, 2, 3 }, ssh.PublicKey.KeyBlob);
    }

    [Fact]
    public void Parses_each_cipher_type_with_database_metadata_and_attachments()
    {
        byte[] keyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        using var baseKey = new TestKey(keyBytes);
        VaultCipherResponse[] cases =
        [
            CreateDto(VaultCipherType.Login, CreateLoginPayload(keyBytes)),
            CreateDto(VaultCipherType.SecureNote, WritePayload(keyBytes, static (writer, key) =>
            {
                writer.WriteStartObject();
                WriteEncrypted(writer, "name", "note", key);
                WriteEncrypted(writer, "notes", "note body", key);
                writer.WriteEndObject();
            })),
            CreateDto(VaultCipherType.Card, WritePayload(keyBytes, static (writer, key) =>
            {
                writer.WriteStartObject();
                WriteEncrypted(writer, "name", "card", key);
                WriteEncrypted(writer, "cardholderName", "holder", key);
                WriteEncrypted(writer, "brand", "brand", key);
                WriteEncrypted(writer, "number", "number", key);
                WriteEncrypted(writer, "expMonth", "month", key);
                WriteEncrypted(writer, "expYear", "year", key);
                WriteEncrypted(writer, "code", "code", key);
                writer.WriteEndObject();
            })),
            CreateDto(VaultCipherType.Identity, WritePayload(keyBytes, static (writer, key) =>
            {
                writer.WriteStartObject();
                WriteEncrypted(writer, "name", "identity", key);
                WriteEncrypted(writer, "title", "title", key);
                WriteEncrypted(writer, "firstName", "first", key);
                WriteEncrypted(writer, "middleName", "middle", key);
                WriteEncrypted(writer, "lastName", "last", key);
                WriteEncrypted(writer, "address1", "address1", key);
                WriteEncrypted(writer, "address2", "address2", key);
                WriteEncrypted(writer, "address3", "address3", key);
                WriteEncrypted(writer, "city", "city", key);
                WriteEncrypted(writer, "state", "state", key);
                WriteEncrypted(writer, "postalCode", "postal", key);
                WriteEncrypted(writer, "country", "country", key);
                WriteEncrypted(writer, "company", "company", key);
                WriteEncrypted(writer, "email", "email", key);
                WriteEncrypted(writer, "phone", "phone", key);
                WriteEncrypted(writer, "ssn", "ssn", key);
                WriteEncrypted(writer, "username", "username", key);
                WriteEncrypted(writer, "passportNumber", "passport", key);
                WriteEncrypted(writer, "licenseNumber", "license", key);
                writer.WriteEndObject();
            })),
            CreateDto(VaultCipherType.SshKey, WritePayload(keyBytes, static (writer, key) =>
            {
                writer.WriteStartObject();
                WriteEncrypted(writer, "name", "ssh", key);
                WriteEncrypted(writer, "privateKey", "private", key);
                WriteEncrypted(writer, "publicKey", "legacy public key", key);
                WriteEncrypted(writer, "keyFingerprint", "fingerprint", key);
                writer.WriteEndObject();
            }))
        ];

        DateTimeOffset deletedDate = DateTimeOffset.Parse("2026-09-28T12:00:00Z");
        for (int i = 0; i < cases.Length; i++)
        {
            cases[i] = cases[i] with
            {
                DeletedDate = deletedDate,
                Attachments = [CreateAttachment($"synthetic-attachment-{i}", $"file-{i}.txt", 12 + i, keyBytes)]
            };
        }

        var parsed = cases.Select(dto =>
            ParseCipher(in dto, dto.Data, baseKey)).ToArray();

        Assert.IsType<LoginVaultCipher>(parsed[0]);
        Assert.Equal("note body", Assert.IsType<SecureNoteVaultCipher>(parsed[1]).Notes);
        var card = Assert.IsType<CardVaultCipher>(parsed[2]);
        Assert.Equal(("holder", "brand", "number", "month", "year", "code"),
            (card.CardholderName, card.Brand, card.Number, card.ExpMonth, card.ExpYear, card.Code));
        var identity = Assert.IsType<IdentityVaultCipher>(parsed[3]);
        Assert.Equal(("title", "first", "middle", "last", "address1", "address2", "address3", "city", "state", "postal", "country", "company", "email", "phone", "ssn", "username", "passport", "license"),
            (identity.Title, identity.FirstName, identity.MiddleName, identity.LastName, identity.Address1, identity.Address2, identity.Address3, identity.City, identity.State, identity.PostalCode, identity.Country, identity.Company, identity.Email, identity.Phone, identity.Ssn, identity.Username, identity.PassportNumber, identity.LicenseNumber));
        var ssh = Assert.IsType<SshKeyVaultCipher>(parsed[4]);
        Assert.Equal(("private", "legacy public key", "fingerprint"), (ssh.PrivateKey, ssh.PublicKey.RawKey, ssh.KeyFingerprint));
        for (int i = 0; i < parsed.Length; i++)
        {
            VaultCipher cipher = parsed[i];
            Assert.Equal(cases[0].Id, cipher.Id);
            Assert.Equal(cases[0].FolderId, cipher.FolderId);
            Assert.Equal(cases[0].Favorite, cipher.Favorite);
            Assert.Equal(cases[0].Reprompt, cipher.Reprompt);
            Assert.Equal(cases[0].RevisionDate, cipher.RevisionDate);
            Assert.Equal(cases[0].CreationDate, cipher.CreationDate);
            Assert.Equal(deletedDate, cipher.DeletedDate);
            VaultCipherAttachment attachment = Assert.Single(cipher.Attachments);
            Assert.Equal(cases[i].Attachments![0].Id, attachment.Id);
            Assert.Equal(cipher.Id, attachment.CipherId);
            Assert.Equal($"file-{i}.txt", attachment.FileName);
            Assert.Equal(cases[i].Attachments![0].Size, attachment.Size);
        }
    }

    [Fact]
    public void Uses_individual_cipher_key_for_payload_and_attachment_name()
    {
        byte[] baseKeyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        byte[] cipherKeyBytes = Enumerable.Range(65, 64).Select(static value => (byte)value).ToArray();
        using var baseKey = new TestKey(baseKeyBytes);
        byte[] payload = CreateLoginPayload(cipherKeyBytes);
        var dto = CreateDto(VaultCipherType.Login, payload) with
        {
            ProtectedCipherKey = EncString.Encrypt(cipherKeyBytes, baseKeyBytes),
            Attachments =
            [
                new()
                {
                    Id = AttachmentId.Parse("synthetic-attachment"),
                    Url = "https://example.invalid/attachment",
                    EncryptedFileName = EncString.Encrypt("synthetic.txt", cipherKeyBytes),
                    ProtectedAttachmentKey = EncString.Empty,
                    Size = BitwardenApi.Primitives.FileSize.FromBytes(12)
                }
            ]
        };

        var parsed = Assert.IsType<LoginVaultCipher>(
            ParseCipher(in dto, payload, baseKey));

        Assert.Equal("synthetic-password", parsed.Password);
        VaultCipherAttachment attachment = Assert.Single(parsed.Attachments);
        Assert.Equal(AttachmentId.Parse("synthetic-attachment"), attachment.Id);
        Assert.Equal(parsed.Id, attachment.CipherId);
        Assert.Equal("synthetic.txt", attachment.FileName);
        Assert.Equal(BitwardenApi.Primitives.FileSize.FromBytes(12), attachment.Size);
    }

    [Fact]
    public void Parses_uri_lists_skips_unknown_nested_members_and_only_the_first_fido2_credential()
    {
        byte[] keyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        using var baseKey = new TestKey(keyBytes);
        byte[] payload = WritePayload(keyBytes, static (writer, key) =>
        {
            writer.WriteStartObject();
            WriteEncrypted(writer, "name", "credential item", key);
            writer.WritePropertyName("uris");
            writer.WriteStartArray();
            writer.WriteStartObject();
            WriteEncrypted(writer, "uri", " https://example.com ", key);
            writer.WriteNumber("match", (int)LoginUri.MatchType.Exact);
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WritePropertyName("fido2Credentials");
            writer.WriteStartArray();
            WriteFido2Credential(writer, key, "12345678-1234-5678-9abc-def012345678");
            writer.WriteStringValue("syntactically valid but not an encrypted credential");
            writer.WriteEndArray();
            writer.WriteEndObject();
        });
        var dto = CreateDto(VaultCipherType.Login, payload);

        var parsed = Assert.IsType<LoginVaultCipher>(
            ParseCipher(in dto, payload, baseKey));

        Assert.Equal("https://example.com", Assert.Single(parsed.Uris).Value);
        Assert.Equal(LoginUri.MatchType.Exact, parsed.Uris[0].Match);
        Fido2Credential credential = parsed.Fido2Credential!;
        Assert.Equal(Guid.Parse("12345678-1234-5678-9abc-def012345678").ToByteArray(bigEndian: true), credential.CredentialId);
        Assert.Equal(Fido2CredentialKeyType.PublicKey, credential.KeyType);
        Assert.Equal(Fido2CredentialKeyAlgorithm.Ecdsa, credential.KeyAlgorithm);
        Assert.Equal(Fido2CredentialKeyCurve.P256, credential.KeyCurve);
        Assert.Equal(new byte[] { 1, 2, 3 }, credential.KeyValue);
        Assert.Equal("example.com", credential.RpId);
        Assert.Equal("Example", credential.RpName);
        Assert.Equal(new byte[] { 1, 2, 3 }, credential.UserHandle);
        Assert.Equal("user", credential.UserName);
        Assert.Equal("User", credential.UserDisplayName);
        Assert.Equal(0u, credential.Counter);
        Assert.False(credential.Discoverable);
        Assert.Equal(DateTimeOffset.Parse("2026-09-30T12:00:00Z"), credential.CreationDate);
    }

    [Fact]
    public void Accepts_uppercase_uri_and_fido2_wire_names()
    {
        byte[] keyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        using var baseKey = new TestKey(keyBytes);
        byte[] payload = WritePayload(keyBytes, static (writer, key) =>
        {
            writer.WriteStartObject();
            WriteEncrypted(writer, "NAME", "uppercase item", key);
            writer.WritePropertyName("URIS");
            writer.WriteStartArray();
            writer.WriteStartObject();
            WriteEncrypted(writer, "URI", "https://uppercase.example", key);
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WritePropertyName("FIDO2CREDENTIALS");
            writer.WriteStartArray();
            WriteFido2Credential(writer, key, "12345678-1234-5678-9abc-def012345678", uppercase: true);
            writer.WriteEndArray();
            writer.WriteEndObject();
        });
        var dto = CreateDto(VaultCipherType.Login, payload);

        var parsed = Assert.IsType<LoginVaultCipher>(ParseCipher(in dto, payload, baseKey));

        Assert.Equal("https://uppercase.example", Assert.Single(parsed.Uris).Value);
        Assert.Equal(LoginUri.MatchType.Domain, parsed.Uris[0].Match);
        Assert.NotNull(parsed.Fido2Credential);
    }

    [Fact]
    public void Duplicate_uri_arrays_replace_previous_values()
    {
        byte[] keyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        using var baseKey = new TestKey(keyBytes);
        byte[] payload = WritePayload(keyBytes, static (writer, key) =>
        {
            writer.WriteStartObject();
            WriteEncrypted(writer, "name", "duplicate URI item", key);
            writer.WritePropertyName("uris");
            writer.WriteStartArray();
            WriteLoginUri(writer, key, "https://discarded.example");
            writer.WriteEndArray();
            writer.WritePropertyName("uris");
            writer.WriteStartArray();
            WriteLoginUri(writer, key, "https://retained.example");
            writer.WriteEndArray();
            writer.WriteEndObject();
        });
        var dto = CreateDto(VaultCipherType.Login, payload);

        var parsed = Assert.IsType<LoginVaultCipher>(ParseCipher(in dto, payload, baseKey));

        Assert.Equal("https://retained.example", Assert.Single(parsed.Uris).Value);
    }

    [Fact]
    public void Login_uri_converter_reuses_the_supplied_list()
    {
        List<LoginUri> activeUris = [];
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = VaultPayloadJsonContext.Default.WithAddedModifier(static typeInfo =>
            {
                if (typeInfo.Type == typeof(LoginUri))
                {
                    JsonPropertyInfo uri = typeInfo.Properties.Single(static property =>
                        string.Equals(property.Name, nameof(LoginUri.Value), StringComparison.OrdinalIgnoreCase));
                    uri.Name = "uri";
                    uri.IsRequired = true;
                }
            })
        };
        var uriTypeInfo = (JsonTypeInfo<LoginUri>)options.GetTypeInfo(typeof(LoginUri));
        var converter = new LoginUrisJsonConverter(() => activeUris, uriTypeInfo);

        List<LoginUri> first = ReadLoginUriArray(converter, "[{\"uri\":\"https://first.example\"}]"u8, options);
        List<LoginUri> second = ReadLoginUriArray(converter, "[{\"uri\":\"https://second.example\"}]"u8, options);

        Assert.Same(activeUris, first);
        Assert.Same(first, second);
        Assert.Equal("https://second.example", Assert.Single(second).Value);
    }

    [Fact]
    public void Rejects_missing_or_null_uri_values_and_invalid_match_values()
    {
        byte[] keyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        using var baseKey = new TestKey(keyBytes);
        byte[] missingUri = "{\"uris\":[{}]}"u8.ToArray();
        byte[] nullUri = "{\"uris\":[{\"uri\":null}]}"u8.ToArray();
        byte[] nullMatch = CreateUriMatchPayload(keyBytes, static writer => writer.WriteNull("match"));
        byte[] stringMatch = CreateUriMatchPayload(keyBytes, static writer => writer.WriteString("match", "exact"));
        byte[] undefinedMatch = CreateUriMatchPayload(keyBytes, static writer => writer.WriteNumber("match", 99));
        byte[] nullUriList = "{\"uris\":null}"u8.ToArray();
        byte[][] payloads = [missingUri, nullUri, nullMatch, stringMatch, undefinedMatch, nullUriList];

        foreach (byte[] payload in payloads)
        {
            var dto = CreateDto(VaultCipherType.Login, payload);
            Assert.ThrowsAny<Exception>(() => ParseCipher(in dto, payload, baseKey));
        }
    }

    [Fact]
    public void Null_and_empty_fido2_arrays_produce_no_credential()
    {
        byte[] keyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        using var baseKey = new TestKey(keyBytes);
        byte[][] payloads = ["{\"fido2Credentials\":null}"u8.ToArray(), "{\"fido2Credentials\":[]}"u8.ToArray()];

        foreach (byte[] payload in payloads)
        {
            var dto = CreateDto(VaultCipherType.Login, payload);
            var parsed = Assert.IsType<LoginVaultCipher>(ParseCipher(in dto, payload, baseKey));
            Assert.Null(parsed.Fido2Credential);
        }
    }

    [Fact]
    public void Rejects_invalid_first_fido2_credential_but_skips_later_values()
    {
        byte[] keyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        using var baseKey = new TestKey(keyBytes);
        byte[] invalidFirstPayload = WritePayload(keyBytes, static (writer, key) =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("fido2Credentials");
            writer.WriteStartArray();
            writer.WriteStringValue("not an object");
            writer.WriteEndArray();
            writer.WriteEndObject();
        });
        byte[] missingRequiredPayload = WritePayload(keyBytes, static (writer, key) =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("fido2Credentials");
            writer.WriteStartArray();
            writer.WriteStartObject();
            WriteEncrypted(writer, "credentialId", "12345678-1234-5678-9abc-def012345678", key);
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteEndObject();
        });
        foreach (byte[] payload in new[] { invalidFirstPayload, missingRequiredPayload })
        {
            var dto = CreateDto(VaultCipherType.Login, payload);
            Assert.ThrowsAny<Exception>(() => ParseCipher(in dto, payload, baseKey));
        }
    }

    [Fact]
    public void Rejects_null_fido2_required_values_including_creation_date()
    {
        byte[] keyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        using var baseKey = new TestKey(keyBytes);
        byte[] nullCredentialIdPayload = WritePayload(keyBytes, static (writer, key) =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("fido2Credentials");
            writer.WriteStartArray();
            WriteFido2Credential(writer, key, "12345678-1234-5678-9abc-def012345678", nullCredentialId: true);
            writer.WriteEndArray();
            writer.WriteEndObject();
        });
        byte[] nullCreationDatePayload = WritePayload(keyBytes, static (writer, key) =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("fido2Credentials");
            writer.WriteStartArray();
            WriteFido2Credential(writer, key, "12345678-1234-5678-9abc-def012345678", nullCreationDate: true);
            writer.WriteEndArray();
            writer.WriteEndObject();
        });
        byte[] missingCreationDatePayload = WritePayload(keyBytes, static (writer, key) =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("fido2Credentials");
            writer.WriteStartArray();
            WriteFido2Credential(writer, key, "12345678-1234-5678-9abc-def012345678", includeCreationDate: false);
            writer.WriteEndArray();
            writer.WriteEndObject();
        });

        foreach (byte[] payload in new[] { nullCredentialIdPayload, nullCreationDatePayload, missingCreationDatePayload })
        {
            var dto = CreateDto(VaultCipherType.Login, payload);
            Assert.ThrowsAny<Exception>(() => ParseCipher(in dto, payload, baseKey));
        }
    }

    [Fact]
    public void Rejects_invalid_roots_truncated_json_and_non_string_encrypted_values()
    {
        byte[] keyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        using var baseKey = new TestKey(keyBytes);
        byte[][] payloads = ["[]"u8.ToArray(), "{"u8.ToArray(), "{\"name\":"u8.ToArray(), "{\"name\":1}"u8.ToArray()];

        foreach (byte[] payload in payloads)
        {
            var dto = CreateDto(VaultCipherType.Login, payload);
            Assert.Throws<JsonException>(() => ParseCipher(in dto, payload, baseKey));
        }
    }

    [Fact]
    public void Parser_recovers_after_authentication_failure_and_separate_instances_isolate_keys()
    {
        byte[] baseKeyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        byte[] firstCipherKey = Enumerable.Range(65, 64).Select(static value => (byte)value).ToArray();
        byte[] secondCipherKey = Enumerable.Range(129, 64).Select(static value => (byte)value).ToArray();
        using var baseKey = new TestKey(baseKeyBytes);
        var parser = new FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing.VaultDataParser();
        byte[] invalidPayload = CreateLoginPayload(secondCipherKey);
        var invalidDto = CreateDto(VaultCipherType.Login, invalidPayload) with
        {
            ProtectedCipherKey = EncString.Encrypt(firstCipherKey, baseKeyBytes)
        };

        Assert.ThrowsAny<Exception>(() => parser.ParseAndDecryptCipher(in invalidDto, invalidPayload, baseKey));

        byte[] validPayload = CreateLoginPayload(secondCipherKey);
        var validDto = CreateDto(VaultCipherType.Login, validPayload) with
        {
            ProtectedCipherKey = EncString.Encrypt(secondCipherKey, baseKeyBytes)
        };
        var recovered = Assert.IsType<LoginVaultCipher>(parser.ParseAndDecryptCipher(in validDto, validPayload, baseKey));
        Assert.Equal("Synthetic item", recovered.Name);

        byte[] otherPayload = CreateLoginPayload(firstCipherKey);
        var otherDto = CreateDto(VaultCipherType.Login, otherPayload) with
        {
            ProtectedCipherKey = EncString.Encrypt(firstCipherKey, baseKeyBytes)
        };
        var isolated = Assert.IsType<LoginVaultCipher>(
            new FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing.VaultDataParser()
                .ParseAndDecryptCipher(in otherDto, otherPayload, baseKey));
        Assert.Equal("Synthetic item", isolated.Name);
    }

    [Fact]
    public void Parser_reuses_contracts_across_cipher_types_and_cipher_keys()
    {
        byte[] baseKeyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        byte[] loginKeyBytes = Enumerable.Range(65, 64).Select(static value => (byte)value).ToArray();
        byte[] noteKeyBytes = Enumerable.Range(129, 64).Select(static value => (byte)value).ToArray();
        using var baseKey = new TestKey(baseKeyBytes);
        var parser = new FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing.VaultDataParser();

        byte[] loginPayload = CreateLoginPayload(loginKeyBytes);
        var loginDto = CreateDto(VaultCipherType.Login, loginPayload) with
        {
            ProtectedCipherKey = EncString.Encrypt(loginKeyBytes, baseKeyBytes)
        };
        var login = Assert.IsType<LoginVaultCipher>(parser.ParseAndDecryptCipher(in loginDto, loginPayload, baseKey));

        byte[] notePayload = WritePayload(noteKeyBytes, static (writer, key) =>
        {
            writer.WriteStartObject();
            WriteEncrypted(writer, "name", "Second key note", key);
            WriteEncrypted(writer, "notes", "second key body", key);
            writer.WriteEndObject();
        });
        var noteDto = CreateDto(VaultCipherType.SecureNote, notePayload) with
        {
            ProtectedCipherKey = EncString.Encrypt(noteKeyBytes, baseKeyBytes)
        };
        var note = Assert.IsType<SecureNoteVaultCipher>(parser.ParseAndDecryptCipher(in noteDto, notePayload, baseKey));

        Assert.Equal("synthetic-password", login.Password);
        Assert.Equal("Second key note", note.Name);
        Assert.Equal("second key body", note.Notes);
    }

    [Fact]
    public void Clears_scratch_after_success_copy_failure_mac_failure_and_callback_failure()
    {
        byte[] key = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        byte[] smallPayload = CreateEncryptedValuePayload(key, "small value");
        byte[] largePayload = CreateEncryptedValuePayload(key, new string('x', 512));

        byte[] smallScratch = CreateDirtyScratch(smallPayload.Length + 64);
        Assert.Equal("small value".Length,
            ParseEncryptedValueInPlace<int>(smallPayload, key, smallScratch, static value => value.Length));
        AssertScratchCleared(smallScratch);

        byte[] largeScratch = CreateDirtyScratch(largePayload.Length + 128);
        Assert.Equal(512,
            ParseEncryptedValueInPlace<int>(largePayload, key, largeScratch, static value => value.Length));
        AssertScratchCleared(largeScratch);

        byte[] wrongKey = Enumerable.Range(65, 64).Select(static value => (byte)value).ToArray();
        smallScratch = CreateDirtyScratch(smallPayload.Length + 64);
        Assert.ThrowsAny<Exception>(() => ParseEncryptedValueInPlace<int>(smallPayload, wrongKey, smallScratch, static value => value.Length));
        AssertScratchCleared(smallScratch);
        largeScratch = CreateDirtyScratch(largePayload.Length + 128);
        Assert.ThrowsAny<Exception>(() => ParseEncryptedValueInPlace<int>(largePayload, wrongKey, largeScratch, static value => value.Length));
        AssertScratchCleared(largeScratch);

        byte[] callbackScratch = CreateDirtyScratch(smallPayload.Length + 64);
        Assert.Throws<InvalidOperationException>(() => ParseEncryptedValueInPlace<int>(
            smallPayload, key, callbackScratch, static _ => throw new InvalidOperationException()));
        AssertScratchCleared(callbackScratch);
        callbackScratch = CreateDirtyScratch(largePayload.Length + 128);
        Assert.Throws<InvalidOperationException>(() => ParseEncryptedValueInPlace<int>(
            largePayload, key, callbackScratch, static _ => throw new InvalidOperationException()));
        AssertScratchCleared(callbackScratch);

        byte[] copyScratch = CreateDirtyScratch(1);
        Assert.ThrowsAny<Exception>(() => ParseEncryptedValueInPlace<int>(smallPayload, key, copyScratch, static value => value.Length));
        AssertScratchCleared(copyScratch);
    }

    [Fact]
    public void String_converter_allocation_delta_is_one_utf8_string_for_short_and_pooled_values()
    {
        byte[] key = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        string[] plaintexts = ["short plaintext", new string('p', 512)];
        foreach (string plaintext in plaintexts)
        {
            byte[] payload = CreateEncryptedValuePayload(key, plaintext);
            var stringConverter = new DecryptedValueJsonConverter<string>(key, VaultPayloadValueParsers.ParseString);
            var lengthConverter = new DecryptedValueJsonConverter<int>(key, static value => value.Length);
            var options = new JsonSerializerOptions();

            long stringBytes = MeasureConverterAllocations(stringConverter, payload, options);
            long lengthBytes = MeasureConverterAllocations(lengthConverter, payload, options);
            long utf8StringBytes = MeasureUtf8StringAllocations(plaintext);

            _output.WriteLine($"Converter allocations for {Encoding.UTF8.GetByteCount(plaintext)} UTF-8 bytes: string={stringBytes} B; length-only={lengthBytes} B; standalone UTF-8 string={utf8StringBytes} B; delta={stringBytes - lengthBytes} B.");
            Assert.Equal(utf8StringBytes, stringBytes - lengthBytes);
        }
    }

    [Fact]
    public void Payload_converters_do_not_write_decrypted_values()
    {
        var stringConverter = new DecryptedValueJsonConverter<string>(ReadOnlyMemory<byte>.Empty,
            VaultPayloadValueParsers.ParseString);
        var matchConverter = new UriMatchTypeJsonConverter();
        var listConverter = new LoginUrisJsonConverter(static () => [], VaultPayloadJsonContext.Default.LoginUri);
        var fidoConverter = new FirstFido2CredentialJsonConverter(VaultPayloadJsonContext.Default.Fido2Credential);
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        var options = new JsonSerializerOptions();

        Assert.Throws<NotSupportedException>(() => stringConverter.Write(writer, "plaintext", options));
        Assert.Throws<NotSupportedException>(() => matchConverter.Write(writer, LoginUri.MatchType.Domain, options));
        Assert.Throws<NotSupportedException>(() => listConverter.Write(writer, [], options));
        Assert.Throws<NotSupportedException>(() => fidoConverter.Write(writer, null, options));
    }

    [Fact]
    public void Rejects_null_required_values_and_invalid_ciphertext_without_echoing_payload()
    {
        byte[] keyBytes = Enumerable.Range(1, 64).Select(static value => (byte)value).ToArray();
        using var baseKey = new TestKey(keyBytes);
        byte[] nullNamePayload = "{\"name\":null}"u8.ToArray();
        var nullNameDto = CreateDto(VaultCipherType.Login, nullNamePayload);
        Assert.Throws<JsonException>(() =>
            ParseCipher(in nullNameDto, nullNamePayload, baseKey));

        byte[] malformedPayload = "{\"name\":\"private-payload-marker\"}"u8.ToArray();
        var malformedDto = CreateDto(VaultCipherType.Login, malformedPayload);
        var error = Assert.ThrowsAny<Exception>(() =>
            ParseCipher(in malformedDto, malformedPayload, baseKey));
        Assert.DoesNotContain("private-payload-marker", error.ToString(), StringComparison.Ordinal);
    }

}

internal static partial class VaultParserTestData
{
    internal static byte[] CreateLoginPayload(ReadOnlySpan<byte> key)
    {
        return WritePayload(key.ToArray(), static (writer, keyBytes) =>
        {
            writer.WriteStartObject();
            WriteEncrypted(writer, "name", "Synthetic item", keyBytes);
            WriteEncrypted(writer, "username", "synthetic-user", keyBytes);
            WriteEncrypted(writer, "password", "synthetic-password", keyBytes);
            writer.WriteEndObject();
        });
    }

    internal static byte[] CreateLoginPayloadWithUriAndFido2(ReadOnlySpan<byte> key)
    {
        return WritePayload(key.ToArray(), static (writer, keyBytes) =>
        {
            writer.WriteStartObject();
            WriteEncrypted(writer, "name", "Synthetic item", keyBytes);
            writer.WritePropertyName("uris");
            writer.WriteStartArray();
            writer.WriteStartObject();
            WriteEncrypted(writer, "uri", "https://example.com", keyBytes);
            writer.WriteNumber("match", (int)LoginUri.MatchType.Domain);
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WritePropertyName("fido2Credentials");
            writer.WriteStartArray();
            WriteFido2Credential(writer, keyBytes, "12345678-1234-5678-9abc-def012345678");
            writer.WriteEndArray();
            writer.WriteEndObject();
        });
    }

    internal static void WriteFido2Credential(
        Utf8JsonWriter writer,
        ReadOnlySpan<byte> key,
        string credentialId,
        bool uppercase = false,
        bool includeCreationDate = true,
        bool nullCredentialId = false,
        bool nullCreationDate = false)
    {
        writer.WriteStartObject();
        string credentialIdProperty = uppercase ? "CREDENTIALID" : "credentialId";
        if (nullCredentialId)
            writer.WriteNull(credentialIdProperty);
        else
            WriteEncrypted(writer, credentialIdProperty, credentialId, key);
        WriteEncrypted(writer, uppercase ? "KEYTYPE" : "keyType", "public-key", key);
        WriteEncrypted(writer, uppercase ? "KEYALGORITHM" : "keyAlgorithm", "ECDSA", key);
        WriteEncrypted(writer, uppercase ? "KEYCURVE" : "keyCurve", "P-256", key);
        WriteEncrypted(writer, uppercase ? "KEYVALUE" : "keyValue", "AQID", key);
        WriteEncrypted(writer, uppercase ? "RPID" : "rpId", "example.com", key);
        WriteEncrypted(writer, uppercase ? "RPNAME" : "rpName", "Example", key);
        WriteEncrypted(writer, uppercase ? "USERHANDLE" : "userHandle", "AQID", key);
        WriteEncrypted(writer, uppercase ? "USERNAME" : "userName", "user", key);
        WriteEncrypted(writer, uppercase ? "USERDISPLAYNAME" : "userDisplayName", "User", key);
        WriteEncrypted(writer, uppercase ? "COUNTER" : "counter", "0", key);
        WriteEncrypted(writer, uppercase ? "DISCOVERABLE" : "discoverable", "false", key);
        if (includeCreationDate)
        {
            string creationDateProperty = uppercase ? "CREATIONDATE" : "creationDate";
            if (nullCreationDate)
                writer.WriteNull(creationDateProperty);
            else
                writer.WriteString(creationDateProperty, "2026-09-30T12:00:00Z");
        }
        writer.WriteStartObject(uppercase ? "UNKNOWNMETADATA" : "unknownMetadata");
        writer.WriteStartArray("nested");
        writer.WriteNumberValue(1);
        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    internal static void WriteLoginUri(Utf8JsonWriter writer, ReadOnlySpan<byte> key, string uri)
    {
        writer.WriteStartObject();
        WriteEncrypted(writer, "uri", uri, key);
        writer.WriteStartObject("unknownMetadata");
        writer.WriteBoolean("unsupported", true);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    internal static byte[] CreateUriMatchPayload(ReadOnlySpan<byte> key, Action<Utf8JsonWriter> writeMatch)
        => WritePayload(key.ToArray(), (writer, keyBytes) =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("uris");
            writer.WriteStartArray();
            writer.WriteStartObject();
            WriteEncrypted(writer, "uri", "https://match.example", keyBytes);
            writeMatch(writer);
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteEndObject();
        });

    internal static byte[] WritePayload(byte[] key, Action<Utf8JsonWriter, byte[]> write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
            write(writer, key);

        return buffer.WrittenSpan.ToArray();
    }

    internal static VaultCipherResponse CreateDto(VaultCipherType type, byte[] payload) => new()
    {
        Id = CipherId.Parse("synthetic-cipher"),
        FolderId = FolderId.Parse("synthetic-folder"),
        VaultCipherType = type,
        RevisionDate = DateTimeOffset.Parse("2026-09-30T12:00:00Z"),
        CreationDate = DateTimeOffset.Parse("2026-09-29T12:00:00Z"),
        Favorite = true,
        Reprompt = false,
        Edit = true,
        ViewPassword = false,
        Data = payload
    };

    internal static VaultCipherAttachmentDownloadResponse CreateAttachment(
        string id,
        string fileName,
        long size,
        ReadOnlySpan<byte> key) => new()
    {
        Id = AttachmentId.Parse(id),
        Url = "https://example.invalid/attachment",
        EncryptedFileName = EncString.Encrypt(fileName, key),
        ProtectedAttachmentKey = EncString.Empty,
        Size = BitwardenApi.Primitives.FileSize.FromBytes(size)
    };

    internal static VaultCipher ParseCipher(
        ref readonly VaultCipherResponse dto,
        ReadOnlySpan<byte> payload,
        SymmetricCryptoKey baseKey)
    {
        var parser = new FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing.VaultDataParser();
        return parser.ParseAndDecryptCipher(in dto, payload, baseKey);
    }

    internal static long MeasureAllocatedBytes(
        FluentBitwarden.AppHost.Modules.Vault.Persistence.Parsing.VaultDataParser parser,
        in VaultCipherResponse dto,
        byte[] payload,
        SymmetricCryptoKey baseKey)
    {
        for (int i = 0; i < 32; i++)
        {
            _ = parser.ParseAndDecryptCipher(in dto, payload, baseKey);
        }

        const int iterations = 100;
        long startingBytes = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < iterations; i++)
        {
            _ = parser.ParseAndDecryptCipher(in dto, payload, baseKey);
        }

        return (GC.GetAllocatedBytesForCurrentThread() - startingBytes) / iterations;
    }

    internal static byte[] CreateEncryptedValuePayload(ReadOnlySpan<byte> key, string plaintext)
        => WritePayload(key.ToArray(), (writer, keyBytes) =>
        {
            writer.WriteStartObject();
            WriteEncrypted(writer, "value", plaintext, keyBytes);
            writer.WriteEndObject();
        });

    internal static T ParseEncryptedValueInPlace<T>(
        byte[] payload,
        ReadOnlySpan<byte> key,
        Span<byte> scratch,
        EncryptedJsonValueReader.DecryptedJsonValueParser<T> parser)
    {
        var reader = new Utf8JsonReader(payload);
        Assert.True(reader.Read());
        Assert.True(reader.Read());
        Assert.True(reader.Read());
        return EncryptedJsonValueReader.ParseInPlace(ref reader, key, scratch, parser);
    }

    internal static List<LoginUri> ReadLoginUriArray(
        LoginUrisJsonConverter converter,
        ReadOnlySpan<byte> payload,
        JsonSerializerOptions options)
    {
        var reader = new Utf8JsonReader(payload);
        Assert.True(reader.Read());
        return converter.Read(ref reader, typeof(List<LoginUri>), options);
    }

    internal static byte[] CreateDirtyScratch(int length) => Enumerable.Repeat((byte)0xA5, length).ToArray();

    internal static void AssertScratchCleared(byte[] scratch)
        => Assert.All(scratch, static value => Assert.Equal((byte)0, value));

    internal static long MeasureConverterAllocations<T>(
        DecryptedValueJsonConverter<T> converter,
        byte[] payload,
        JsonSerializerOptions options)
    {
        const int warmups = 32;
        const int iterations = 100;
        for (int i = 0; i < warmups; i++)
            _ = ReadConverterValue(converter, payload, options);

        long start = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < iterations; i++)
            _ = ReadConverterValue(converter, payload, options);

        return (GC.GetAllocatedBytesForCurrentThread() - start) / iterations;
    }

    internal static long MeasureUtf8StringAllocations(string plaintext)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(plaintext);
        const int warmups = 32;
        const int iterations = 100;
        for (int i = 0; i < warmups; i++)
            _ = Encoding.UTF8.GetString(utf8.AsSpan());

        long start = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < iterations; i++)
            _ = Encoding.UTF8.GetString(utf8.AsSpan());

        return (GC.GetAllocatedBytesForCurrentThread() - start) / iterations;
    }

    private static T ReadConverterValue<T>(
        DecryptedValueJsonConverter<T> converter,
        byte[] payload,
        JsonSerializerOptions options)
    {
        var reader = new Utf8JsonReader(payload);
        reader.Read();
        reader.Read();
        reader.Read();
        return converter.Read(ref reader, typeof(T), options);
    }

    internal static void WriteEncrypted(Utf8JsonWriter writer, string propertyName, string value, ReadOnlySpan<byte> key)
    {
        writer.WritePropertyName(propertyName);
        JsonSerializer.Serialize(writer, EncString.Encrypt(value, key), VaultParserTestJsonContext.Default.EncString);
    }

    internal sealed class TestKey(byte[] keyBytes) : SymmetricCryptoKey(keyBytes);

    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(EncString))]
    private partial class VaultParserTestJsonContext : JsonSerializerContext;
}
