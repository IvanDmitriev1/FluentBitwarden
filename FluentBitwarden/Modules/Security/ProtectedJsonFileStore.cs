using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace FluentBitwarden.Modules.Security;

internal static class ProtectedJsonFileStore
{
    private static readonly byte[] OptionalEntropy =
    [
        0x44, 0x65, 0x76, 0x69, 0x63, 0x65, 0x49, 0x64
    ];

    public static void Store<T>(string filePath, T value, JsonTypeInfo<T> jsonTypeInfo)
    {
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(value, jsonTypeInfo);
        var protectedBytes = ProtectedData.Protect(jsonBytes, OptionalEntropy, DataProtectionScope.CurrentUser);

        try
        { 
            File.WriteAllBytes(filePath, protectedBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(protectedBytes);
            CryptographicOperations.ZeroMemory(jsonBytes);
        }
    }

    public static T? Load<T>(string filePath, JsonTypeInfo<T> jsonTypeInfo)
    {
        if (!File.Exists(filePath))
        {
            return default;
        }

        byte[] protectedBytes = File.ReadAllBytes(filePath);
        byte[] jsonBytes = ProtectedData.Unprotect(protectedBytes, OptionalEntropy, DataProtectionScope.CurrentUser);

        try
        {
            return JsonSerializer.Deserialize(jsonBytes, jsonTypeInfo);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(protectedBytes);
            CryptographicOperations.ZeroMemory(jsonBytes);
        }
    }

}