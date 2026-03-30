using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;

namespace FluentBitwarden.Modules.Account.Models;

public sealed record StoredAccount(
    UserId UserId,
    string Email,
    BitwardenEnvironment Environment,
    AccountCryptoMaterial AccountCryptoMaterial,
    DateTimeOffset LastSyncAt,
    bool HasPin,
    bool HasWindowsHello);
