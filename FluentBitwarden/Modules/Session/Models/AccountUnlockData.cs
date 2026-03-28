using BitwardenApi.Modules.Identity.Models;
using BitwardenApi.Shared.Cryptography;

namespace FluentBitwarden.Modules.Session.Models;

public sealed record AccountUnlockData(
    KdfConfig KdfConfig,
    EncryptedUserKey EncryptedUserKey,
    string Salt);