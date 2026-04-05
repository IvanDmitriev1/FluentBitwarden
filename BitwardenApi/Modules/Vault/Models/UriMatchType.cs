namespace BitwardenApi.Modules.Vault.Models;

public enum UriMatchType
{
    Domain = 0,
    Host = 1,
    StartsWith = 2,
    Exact = 3,
    RegularExpression = 4,
    Never = 5,
}