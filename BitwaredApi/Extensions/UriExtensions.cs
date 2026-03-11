namespace BitwaredApi.Extensions;

internal static class UriExtensions
{
    public static Uri AppendRelativePath(this Uri baseAddress, string relativePath)
        => new(baseAddress, relativePath);
}
