using System.Text.Json.Serialization;
using BitwardenApi.Primitives;
using BitwardenApi.Vault.Items.Contracts;

namespace FluentBitwarden.AppHost.Modules.Vault.Persistence.Serialization;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(LoginVaultCipher))]
[JsonSerializable(typeof(SecureNoteVaultCipher))]
[JsonSerializable(typeof(CardVaultCipher))]
[JsonSerializable(typeof(IdentityVaultCipher))]
[JsonSerializable(typeof(SshKeyVaultCipher))]
[JsonSerializable(typeof(LoginUri))]
[JsonSerializable(typeof(List<LoginUri>))]
[JsonSerializable(typeof(Fido2Credential))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(TotpValue))]
[JsonSerializable(typeof(OpenSshPublicKey))]
[JsonSerializable(typeof(uint))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(byte[]))]
[JsonSerializable(typeof(DateTimeOffset))]
internal partial class VaultPayloadJsonContext : JsonSerializerContext;
