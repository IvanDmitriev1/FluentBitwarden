using System.Text.Json.Serialization;

using BitwardenApi.Vault.Ciphers.Models;

namespace BitwardenApi.Vault.Ciphers.Serialization;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    GenerationMode = JsonSourceGenerationMode.Metadata,
    RespectNullableAnnotations = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(LoginCipherDataPayload))]
[JsonSerializable(typeof(SecureNoteCipherDataPayload))]
[JsonSerializable(typeof(CardCipherDataPayload))]
[JsonSerializable(typeof(IdentityCipherDataPayload))]
[JsonSerializable(typeof(SshKeyCipherDataPayload))]
[JsonSerializable(typeof(EncryptedCipherField))]
[JsonSerializable(typeof(EncryptedPasswordHistory))]
[JsonSerializable(typeof(EncryptedLoginUri))]
[JsonSerializable(typeof(EncryptedFido2Credential))]
internal sealed partial class CipherDataPayloadJsonContext : JsonSerializerContext;
