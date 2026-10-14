using BitwardenApi.Primitives;
using FluentBitwarden.AppHost.Modules.Account.Contracts;

namespace FluentBitwarden.AppHost.Modules.Account.Internal;

internal sealed class AccountTokenCache
{
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private readonly Dictionary<BitwardenAccountContext, AccountSessionTokens> _tokens = [];

    public async ValueTask<Entry> AcquireAsync(
        BitwardenAccountContext context,
        CancellationToken cancellationToken)
    {
        await _refreshGate.WaitAsync(cancellationToken);
        return new Entry(this, context);
    }

    private bool TryGetValidAccessToken(BitwardenAccountContext context, out SessionAccessToken accessToken)
    {
        if (_tokens.TryGetValue(context, out AccountSessionTokens? tokens) && tokens.IsValid())
        {
            accessToken = tokens.AccessToken;
            return true;
        }

        accessToken = default;
        return false;
    }

    private void Store(BitwardenAccountContext context, AccountSessionTokens tokens) =>
        _tokens[context] = tokens;

    internal sealed class Entry(AccountTokenCache cache, BitwardenAccountContext context) : IDisposable
    {
        private int _disposed;

        public bool TryGetValidAccessToken(out SessionAccessToken accessToken)
        {
            ThrowIfDisposed();
            return cache.TryGetValidAccessToken(context, out accessToken);
        }

        public void Store(AccountSessionTokens tokens)
        {
            ThrowIfDisposed();
            cache.Store(context, tokens);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                cache._refreshGate.Release();
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(nameof(Entry));
        }
    }
}
