using MemoryPack;

namespace BitwardenApi.Tests.Tests;

public sealed class MemoryPackCompatibilityTests
{
    [Fact]
    public void Bitwarden_environment_deserializes_legacy_service_bases_without_a_wire_schema_change()
    {
        byte[] bytes = Convert.FromHexString(
            "04EDFFFFFF1200000068747470733A2F2F612E746573742F617069E8FFFFFF1700000068747470733A2F2F692E746573742F6964656E74697479E3FFFFFF1C00000068747470733A2F2F6E2E746573742F6E6F74696669636174696F6E73EBFFFFFF1400000068747470733A2F2F762E746573742F7661756C74");
        BitwardenEnvironment environment = MemoryPackSerializer.Deserialize<BitwardenEnvironment>(bytes)!;

        Assert.Equal("https://a.test/api/", environment.ApiBase.AbsoluteUri);
        Assert.Equal("https://i.test/identity/", environment.IdentityBase.AbsoluteUri);
        Assert.Equal("https://n.test/notifications/", environment.NotificationsBase.AbsoluteUri);
        Assert.Equal("https://v.test/vault", environment.VaultBase.AbsoluteUri);
    }

    [Fact]
    public void Login_cipher_without_totp_preserves_absence_across_memorypack()
    {
        var login = (LoginCipher)Cipher.CreateBlankCipher(CipherType.Login);
        login.Id = CipherId.Parse("11111111-1111-1111-1111-111111111111");
        login.Name = "without OTP";

        byte[] bytes = MemoryPackSerializer.Serialize<Cipher>(login);
        var restored = Assert.IsType<LoginCipher>(MemoryPackSerializer.Deserialize<Cipher>(bytes));

        Assert.Null(restored.Totp);
        Assert.Equal(login.Id, restored.Id);
        Assert.Equal("without OTP", restored.Name);
        Assert.Equal(bytes, MemoryPackSerializer.Serialize<Cipher>(restored));
    }

    [Fact]
    public void Legacy_two_factor_bytes_decode_and_reserialize_identically()
    {
        byte[] proofBytes = Convert.FromHexString("02F9FFFFFF0600000031323334353600000000");
        IdentityTwoFactorProof proof = MemoryPackSerializer.Deserialize<IdentityTwoFactorProof>(proofBytes);
        Assert.Equal("123456", proof.Code);
        Assert.Equal(IdentityTwoFactorProviderType.Authenticator, proof.Provider);
        Assert.Equal(proofBytes, MemoryPackSerializer.Serialize(proof));

        byte[] challengeBytes = Convert.FromHexString("01020000000000000001000000");
        IdentityTwoFactorChallenge challenge = MemoryPackSerializer.Deserialize<IdentityTwoFactorChallenge>(challengeBytes);
        Assert.Equal([IdentityTwoFactorProviderType.Authenticator, IdentityTwoFactorProviderType.Email], challenge.Providers.Select(option => option.Provider));
        Assert.Equal(challengeBytes, MemoryPackSerializer.Serialize(challenge));
    }
}
