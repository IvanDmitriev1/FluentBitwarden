namespace BitwardenApi.Cryptography.KeyDerivation;

public abstract record KdfConfig
{
    public sealed record Pbkdf2(int Iterations) : KdfConfig;
    public sealed record Argon2Id(int Iterations, int MemoryMib, int Parallelism) : KdfConfig;
}