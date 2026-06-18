using BeachBot.Core.Abstractions;

namespace BeachBot.Tests;

/// <summary>A clock whose delays are released manually by <see cref="Advance"/>.</summary>
internal sealed class SignalClock : IClock
{
    private readonly object _gate = new();
    private readonly List<TaskCompletionSource> _waiters = new();

    public SignalClock(DateTimeOffset now) => UtcNow = now;

    public DateTimeOffset UtcNow { get; private set; }

    public Task Delay(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        lock (_gate)
            _waiters.Add(tcs);
        return tcs.Task;
    }

    /// <summary>Moves time forward and releases every currently pending delay.</summary>
    public void Advance(TimeSpan by)
    {
        TaskCompletionSource[] toRelease;
        lock (_gate)
        {
            UtcNow += by;
            toRelease = _waiters.ToArray();
            _waiters.Clear();
        }
        foreach (var waiter in toRelease)
            waiter.TrySetResult();
    }

    public async Task WaitForPendingDelayAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            lock (_gate)
                if (_waiters.Count > 0)
                    return;
            await Task.Delay(10);
        }
        throw new TimeoutException("No pending delay was registered in time.");
    }
}

/// <summary>A clock with a fixed time whose delays complete immediately.</summary>
internal sealed class ImmediateClock : IClock
{
    public ImmediateClock(DateTimeOffset now) => UtcNow = now;
    public DateTimeOffset UtcNow { get; set; }
    public Task Delay(TimeSpan delay, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
