using BitwardenApi.Models;
using FluentBitwarden.Modules.Session.Models;

namespace FluentBitwarden.Modules.Account.Models;

public sealed record AccountProfile(
    UserId UserId,
    string Email,
    BitwardenEnvironment Environment,
    DateTimeOffset LastSyncAt);
