using System.Text.Json.Serialization;

using BitwardenApi.Primitives;
using BitwardenApi.Vault.Ciphers.Models;

namespace BitwardenApi.Vault.Ciphers.Serialization;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(LoginCipher))]
[JsonSerializable(typeof(SecureNoteCipher))]
[JsonSerializable(typeof(CardCipher))]
[JsonSerializable(typeof(IdentityCipher))]
[JsonSerializable(typeof(SshKeyCipher))]
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
