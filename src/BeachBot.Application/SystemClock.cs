using BeachBot.Core.Abstractions;

namespace BeachBot.Application;

/// <summary>The real wall-clock implementation of <see cref="IClock"/>.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public Task Delay(TimeSpan delay, CancellationToken cancellationToken = default)
        => Task.Delay(delay < TimeSpan.Zero ? TimeSpan.Zero : delay, cancellationToken);
}
