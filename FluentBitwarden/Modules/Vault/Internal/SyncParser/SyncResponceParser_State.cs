namespace FluentBitwarden.Modules.Vault.Internal.SyncParser;

internal partial class VaultSyncResponseParser
{
    public readonly record struct SyncParserReport(int Ciphers, int Folders, int Collections);

    private struct ArrayCaptureState
    {
        public bool IsActive { get; set; }
        public int ProcessedItems { get; set; }
    }

    private struct ObjectCaptureState
    {
        public bool IsActive { get; set; }
        public int Depth { get; set; }
    }
}
