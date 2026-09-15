namespace BitwardenApi.Identity.Contracts;

public readonly record struct ProtectedUserKey(EncString Value)
{
    public static ProtectedUserKey Create(EncString value) => new(value);

    public byte[] Decrypt(ReadOnlySpan<char> masterPassword, ReadOnlySpan<char> salt, KdfConfig kdfConfig)
    {
        using var masterKey = MasterKey.Derive(masterPassword, salt, kdfConfig);
        using var stretchedMasterKey = masterKey.Stretch();

        return Value.DecodeToArray(stretchedMasterKey.Span);
    }
}

public readonly record struct ProtectedPrivateKey(EncString Value)
{
    public static ProtectedPrivateKey Create(EncString value) => new(value);
}
