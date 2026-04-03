using BitwardenApi.Modules.Vault.Abstractions;

namespace BitwardenApi.Modules.Vault.SyncParser;

public sealed class SyncPayload : IAsyncDisposable
{
    internal SyncPayload(HttpResponseMessage response, Stream content)
    {
        _response = response;
        _content = content;
    }

    private readonly HttpResponseMessage _response;
    private readonly Stream _content;

    public async ValueTask DisposeAsync()
    {
        await _content.DisposeAsync();
        _response.Dispose();
    }

    public Task<SyncResponceParser.SyncParserReport> ParseAsync(ISyncDataWriter dataWriter)
    {
        return SyncResponceParser.ParseAsync(dataWriter, _content, CancellationToken.None);
    }
}