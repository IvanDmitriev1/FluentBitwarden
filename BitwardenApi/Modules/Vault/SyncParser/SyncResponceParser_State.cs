namespace BitwardenApi.Modules.Vault.SyncParser;

public partial class SyncResponseParser
{
    public readonly struct SyncParserReport
    {
        public SyncParserReport(int ciphers, int folders, int collections)
        {
            Ciphers = ciphers;
            Folders = folders;
            Collections = collections;
        }

        public int Ciphers { get; }
        public int Folders { get; }
        public int Collections { get; }
    }

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
