using BitwardenApi.Cryptography.Encryption;
namespace BitwardenApi.Vault.Ciphers.Requests;
public sealed class CipherCardRequest
{
    public EncString CardholderName { get; init; }
    public EncString Brand { get; init; }
    public EncString Number { get; init; }
    public EncString ExpMonth { get; init; }
    public EncString ExpYear { get; init; }
    public EncString Code { get; init; }
}
