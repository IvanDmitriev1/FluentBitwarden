using BitwardenApi.Cryptography.KeyDerivation;
using BitwardenApi.Cryptography.Keys;
namespace BitwardenApi.Identity.Models;


public sealed record MasterPasswordUnlockData(KdfConfig KdfConfig, string Salt, ProtectedUserKey UserKey);
