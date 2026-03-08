namespace BitwaredApi.Http;

internal static class HttpRequestOptionKeys
{
    public static readonly HttpRequestOptionsKey<bool> SkipAuthorization = new("Bitwared.SkipAuthorization");
}
