namespace FluentBitwarden.Abstractions.Storage;

internal interface IVaultAccountClearStore
{
    ValueTask ClearAccountAsync(
        string accountId,
        CancellationToken cancellationToken = default);
}
