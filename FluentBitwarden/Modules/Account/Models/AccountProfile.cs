using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Context;

namespace FluentBitwarden.Modules.Account.Models;

public sealed record AccountProfile(
    UserId UserId,
    string Email,
    BitwardenEnvironment Environment,
    DateTimeOffset LastSyncAt);
