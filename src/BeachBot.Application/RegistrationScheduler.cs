using System.Collections.Concurrent;
using BeachBot.Core.Abstractions;

namespace BeachBot.Application;

/// <summary>
/// Fires a callback when a registration's open time arrives. Long waits are split
/// into chunks so we re-check the clock periodically (robust against sleep/clock
/// changes and the Task.Delay range limit). One job per registration id.
/// </summary>
public sealed class RegistrationScheduler : IAsyncDisposable
{
    private static readonly TimeSpan MaxChunk = TimeSpan.FromHours(1);

    private readonly IClock _clock;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _jobs = new();

    public RegistrationScheduler(IClock clock) => _clock = clock;

    public bool IsScheduled(string id) => _jobs.ContainsKey(id);

    /// <summary>Schedules (or reschedules) <paramref name="id"/> to fire at <paramref name="fireAt"/>.</summary>
    public void Schedule(string id, DateTimeOffset fireAt, Func<CancellationToken, Task> onFire)
    {
        Cancel(id);
        var cts = new CancellationTokenSource();
        _jobs[id] = cts;
        _ = RunAsync(id, fireAt, onFire, cts.Token);
    }

    public bool Cancel(string id)
    {
        if (_jobs.TryRemove(id, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
            return true;
        }
        return false;
    }

    private async Task RunAsync(string id, DateTimeOffset fireAt, Func<CancellationToken, Task> onFire, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var remaining = fireAt - _clock.UtcNow;
                if (remaining <= TimeSpan.Zero)
                    break;
                await _clock.Delay(remaining < MaxChunk ? remaining : MaxChunk, ct).ConfigureAwait(false);
            }

            ct.ThrowIfCancellationRequested();
            await onFire(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Cancelled before firing — nothing to do.
        }
        finally
        {
            _jobs.TryRemove(id, out _);
        }
    }

    public ValueTask DisposeAsync()
    {
        foreach (var cts in _jobs.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _jobs.Clear();
        return ValueTask.CompletedTask;
    }
}
