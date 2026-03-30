using System.Security.Cryptography;

namespace FluentBitwarden.Modules.Security.Internal;

internal static class TpmRsaFactory
{
    private const string TpmProviderName = "Microsoft Platform Crypto Provider";
    private const string TpmKeyName = "bw_session_key_v1";
    private const int RsaKeySizeBits = 2048;

    public static RSA OpenRsa() => new RSACng(OpenOrCreateKey());

    private static CngKey OpenOrCreateKey()
    {
        var provider = new CngProvider(TpmProviderName);
        if (CngKey.Exists(TpmKeyName, provider))
        {
            return CngKey.Open(TpmKeyName, provider);
        }

        var creationParameters = new CngKeyCreationParameters
        {
            Provider = provider,
            KeyUsage = CngKeyUsages.AllUsages,
            ExportPolicy = CngExportPolicies.None
        };

        creationParameters.Parameters.Add(
            new CngProperty("Length", BitConverter.GetBytes(RsaKeySizeBits), CngPropertyOptions.None));

        return CngKey.Create(CngAlgorithm.Rsa, TpmKeyName, creationParameters);
    }
}
