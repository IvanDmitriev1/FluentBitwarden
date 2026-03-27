using BitwardenApi.Primitives;

namespace FluentBitwarden.Modules.Accounts.Models;

[StronglyTypedId(Template.Guid, "guid-dapper")]
public partial struct AccountProfileId;

public sealed record AccountProfile(
    AccountProfileId AccountId,
    UserId UserId,
    string Email,
    string? Name,
    string? SecurityStamp);
