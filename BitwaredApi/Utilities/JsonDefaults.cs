using System.Text.Json;

namespace BitwaredApi.Utilities;

internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };
}
