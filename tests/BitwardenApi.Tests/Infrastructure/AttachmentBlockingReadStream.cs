namespace BitwardenApi.Tests.Infrastructure;

internal sealed class AttachmentBlockingReadStream : Stream
{
    private readonly TaskCompletionSource _readStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task ReadStarted => _readStarted.Task;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() => throw new NotSupportedException();
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => WaitForCancellationAsync(cancellationToken);

    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
        => WaitForCancellationAsync(cancellationToken).AsTask();

    private async ValueTask<int> WaitForCancellationAsync(CancellationToken cancellationToken)
    {
        _readStarted.TrySetResult();
        return await new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously)
            .Task.WaitAsync(cancellationToken);
    }
}
