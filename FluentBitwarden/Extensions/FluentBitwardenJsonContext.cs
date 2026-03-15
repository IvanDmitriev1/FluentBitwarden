using System.Text.Json;
using System.Text.Json.Serialization;
using BitwaredApi.Models.Auth;
using FluentBitwarden.Models.Settings;
using FluentBitwarden.Models.Vault;

namespace FluentBitwarden.Extensions;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    WriteIndented = false)]
[JsonSerializable(typeof(PersistableSession))]
[JsonSerializable(typeof(AppSettingsDocument))]
[JsonSerializable(typeof(LocalVaultState))]
[JsonSerializable(typeof(EncryptedUserKeyPayload))]
[JsonSerializable(typeof(MasterPasswordLocalVaultKeyState))]
[JsonSerializable(typeof(WindowsHelloLocalVaultKeyState))]
[JsonSerializable(typeof(PinLocalVaultKeyState))]
internal sealed partial class FluentBitwardenJsonContext : JsonSerializerContext
{
}
