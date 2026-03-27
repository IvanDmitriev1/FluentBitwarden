using BitwardenApi.Context;
using BitwardenApi.Primitives;

namespace BitwardenApi.Vault;

public sealed record GetSyncRequest(
    BitwardenClientContext Context,
    AccessToken AccessToken,
    bool ExcludeDomains = false);

public sealed record GetCipherRequest(
    BitwardenClientContext Context,
    AccessToken AccessToken,
    CipherId CipherId);

public sealed record GetAllCiphersRequest(
    BitwardenClientContext Context,
    AccessToken AccessToken);

/// <summary>
/// Creates a cipher from raw JSON content.
/// </summary>
/// <param name="Content">
/// Cipher payload stream. This stream is consumed and disposed by the API call.
/// </param>
public sealed record CreateCipherRequest(
    BitwardenClientContext Context,
    AccessToken AccessToken,
    Stream Content);

/// <summary>
/// Updates a cipher from raw JSON content.
/// </summary>
/// <param name="Content">
/// Cipher payload stream. This stream is consumed and disposed by the API call.
/// </param>
public sealed record UpdateCipherRequest(
    BitwardenClientContext Context,
    AccessToken AccessToken,
    CipherId CipherId,
    Stream Content);

public sealed record DeleteCipherRequest(
    BitwardenClientContext Context,
    AccessToken AccessToken,
    CipherId CipherId);
