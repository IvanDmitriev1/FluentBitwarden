using BitwardenApi.Modules.Identity.Models;

namespace FluentBitwarden.Modules.Account.Models;

public sealed record StoredAccountSecurity(
    UserId UserId,
    bool HasPin,
    bool HasWindowsHello);
