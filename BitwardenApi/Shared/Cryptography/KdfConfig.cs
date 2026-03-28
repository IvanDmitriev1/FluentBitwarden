namespace BitwardenApi.Shared.Cryptography;

public enum KdfType
{
    Pbkdf2Sha256 = 0,
    Argon2Id = 1,
}

public abstract record KdfConfig
{
    public sealed record Pbkdf2(int Iterations) : KdfConfig;
    public sealed record Argon2Id(int Iterations, int MemoryMib, int Parallelism) : KdfConfig;
}