namespace BeachBot.Core.Abstractions;

/// <summary>
/// Abstraction over the system clock so scheduling logic can be unit-tested
/// without waiting for real time to pass.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }

    Task Delay(TimeSpan delay, CancellationToken cancellationToken = default);
}
