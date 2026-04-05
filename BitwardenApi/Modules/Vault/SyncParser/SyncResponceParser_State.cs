using CommunityToolkit.HighPerformance.Buffers;

namespace BitwardenApi.Modules.Vault.SyncParser;

public partial class SyncResponceParser
{
    public readonly record struct SyncParserReport(int Ciphers, int Folders, int Collections);

    private struct ArrayCaptureState
    {
        public bool IsActive { get; set; }
        public int ProcessedItems { get; set; }
    }

    private class ObjectCaptureState : IDisposable
    {
        private MemoryOwner<byte> _payloadMemoryOwner = MemoryOwner<byte>.Allocate(1024 * 4);

        public bool IsActive { get; set; }
        public int Depth { get; set; }
        public Span<byte> PayloadSpan => _payloadMemoryOwner.Span;


        public void ResizePayloadMemoryOwner(int newSize)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(newSize);

            if (_payloadMemoryOwner.Length >= newSize)
                return;

            _payloadMemoryOwner.Dispose();
            _payloadMemoryOwner = MemoryOwner<byte>.Allocate(newSize);
        }

        public void Dispose()
        {
            _payloadMemoryOwner.Dispose();
        }
    }
}