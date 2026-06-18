namespace BeachBot.Core.Models;

/// <summary>
/// A team already registered for a tournament. Used to detect whether the
/// current user (or a chosen partner) is already signed up.
/// </summary>
public sealed class Team
{
    public required string Id { get; init; }

    /// <summary>Ids of the players that make up this team.</summary>
    public required IReadOnlyList<string> PlayerIds { get; init; }
}
