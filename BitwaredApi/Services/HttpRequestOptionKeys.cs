namespace BitwaredApi.Services;

public static class HttpRequestOptionKeys
{
    public static readonly HttpRequestOptionsKey<bool> SkipAuthorization = new("Bitwared.SkipAuthorization");
}
