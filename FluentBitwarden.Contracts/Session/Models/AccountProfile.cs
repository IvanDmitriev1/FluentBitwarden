using BitwardenApi.Models;

namespace FluentBitwarden.Contracts.Session.Models;

public sealed record AccountProfile(
    UserId UserId,
    string Email,
    BitwardenEnvironment Environment,
    DateTimeOffset LastSyncAt);