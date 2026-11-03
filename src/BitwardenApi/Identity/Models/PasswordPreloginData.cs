using BitwardenApi.Cryptography.KeyDerivation;

namespace BitwardenApi.Identity.Models;

public sealed record PasswordPreloginData(KdfConfig KdfConfig, string? Salt);
