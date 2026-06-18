using BeachBot.Application;
using Xunit;

namespace BeachBot.Tests;

public class RegistrationSchedulerTests
{
    [Fact]
    public async Task FiresImmediately_WhenAlreadyDue()
    {
        var clock = new ImmediateClock(DateTimeOffset.UtcNow);
        await using var scheduler = new RegistrationScheduler(clock);

        var fired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        scheduler.Schedule("a", clock.UtcNow.AddMinutes(-1), _ => { fired.TrySetResult(); return Task.CompletedTask; });

        await fired.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task FiresWhenTimeArrives_AcrossChunks()
    {
        var start = DateTimeOffset.UtcNow;
        var clock = new SignalClock(start);
        await using var scheduler = new RegistrationScheduler(clock);

        var fired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        scheduler.Schedule("a", start.AddHours(3), _ => { fired.TrySetResult(); return Task.CompletedTask; });

        await clock.WaitForPendingDelayAsync(TimeSpan.FromSeconds(2));
        Assert.False(fired.Task.IsCompleted); // still waiting

        clock.Advance(TimeSpan.FromHours(3)); // now due
        await fired.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Cancel_PreventsFiring()
    {
        var clock = new SignalClock(DateTimeOffset.UtcNow);
        await using var scheduler = new RegistrationScheduler(clock);

        var fired = false;
        scheduler.Schedule("a", clock.UtcNow.AddHours(1), _ => { fired = true; return Task.CompletedTask; });

        await clock.WaitForPendingDelayAsync(TimeSpan.FromSeconds(2));
        Assert.True(scheduler.IsScheduled("a"));

        scheduler.Cancel("a");
        await Task.Delay(100);

        Assert.False(fired);
        Assert.False(scheduler.IsScheduled("a"));
    }
}
