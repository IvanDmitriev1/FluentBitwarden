namespace BitwardenApi.Tests.Infrastructure;

internal sealed class NotificationTestTimeProvider : TimeProvider
{
    private readonly object _gate = new();
    private readonly List<TimeSpan> _scheduledDelays = [];
    private TaskCompletionSource<ManualTimer> _nextTimer = CreateSignal();
    private DateTimeOffset _utcNow = DateTimeOffset.UnixEpoch;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public IReadOnlyList<TimeSpan> ScheduledDelays
    {
        get
        {
            lock (_gate)
            {
                return _scheduledDelays.ToArray();
            }
        }
    }

    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate)
        {
            return _utcNow;
        }
    }

    public override long GetTimestamp() => GetUtcNow().Ticks;

    public override ITimer CreateTimer(
        TimerCallback callback,
        object? state,
        TimeSpan dueTime,
        TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state);
        timer.Change(dueTime, period);
        return timer;
    }

    public async Task<TimeSpan> WaitForNextTimerAsync(CancellationToken cancellationToken)
    {
        TaskCompletionSource<ManualTimer> signal;
        lock (_gate)
        {
            signal = _nextTimer;
        }

        ManualTimer timer = await signal.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        return timer.DueTime;
    }

    public async Task<TimeSpan> AdvanceNextTimerAsync(CancellationToken cancellationToken)
    {
        TaskCompletionSource<ManualTimer> signal;
        lock (_gate)
        {
            signal = _nextTimer;
        }

        ManualTimer timer = await signal.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

        lock (_gate)
        {
            if (ReferenceEquals(_nextTimer, signal))
            {
                _nextTimer = CreateSignal();
            }

            _utcNow += timer.DueTime;
            _scheduledDelays.Add(timer.DueTime);
        }

        timer.Fire();
        return timer.DueTime;
    }

    private void Schedule(ManualTimer timer, TimeSpan dueTime)
    {
        if (dueTime == Timeout.InfiniteTimeSpan)
        {
            return;
        }

        TaskCompletionSource<ManualTimer> signal;
        lock (_gate)
        {
            timer.DueTime = dueTime;
            signal = _nextTimer;
        }

        signal.TrySetResult(timer);
    }

    private static TaskCompletionSource<ManualTimer> CreateSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class ManualTimer(
        NotificationTestTimeProvider owner,
        TimerCallback callback,
        object? state) : ITimer
    {
        private bool _disposed;

        public TimeSpan DueTime { get; set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            if (_disposed)
            {
                return false;
            }

            // shortcut: only one-shot retry delays are scheduled; add periodic firing if this helper gains another consumer.
            owner.Schedule(this, dueTime);
            return true;
        }

        public void Dispose() => _disposed = true;

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        public void Fire()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            callback(state);
        }
    }
}
