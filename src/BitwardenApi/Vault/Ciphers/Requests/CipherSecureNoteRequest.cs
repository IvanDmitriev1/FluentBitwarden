namespace BitwardenApi.Vault.Ciphers.Requests;
public sealed class CipherSecureNoteRequest
{
    /// <summary>Secure note type: 0 = generic.</summary>
    public required int Type { get; init; }
}
