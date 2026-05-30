using BitwardenApi.Models;

namespace FluentBitwarden.Contracts.Session.Models;

[MemoryPackable]
public sealed partial record AccountProfile(
    UserId UserId,
    string Email,
    BitwardenEnvironment Environment,
    DateTimeOffset LastSyncAt);