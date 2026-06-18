namespace BeachBot.Core.Models;

/// <summary>
/// A beach volleyball tournament. The list endpoint returns a light form, while
/// <c>tournament.getDetail</c> fills in the scheduling-critical fields such as
/// <see cref="RegistrationOpensAt"/>. Fields that are only known after fetching
/// the detail are nullable.
/// </summary>
public sealed class Tournament
{
    public required string Id { get; init; }

    /// <summary>Display name (the site's resolved name, falling back to the city).</summary>
    public required string Name { get; init; }

    public required string City { get; init; }

    public string? SeriesId { get; init; }
    public string? SeriesName { get; init; }
    public string? CategoryId { get; init; }
    public string? CategoryName { get; init; }

    /// <summary>Gender constraint from the series (e.g. "male", "female", "mixed"), if any.</summary>
    public string? Gender { get; init; }

    public int TeamSize { get; init; }
    public int MaxTeams { get; init; }

    /// <summary>How many teams have registered so far. Zero usually means registration has not opened yet.</summary>
    public int TeamCountCurrent { get; init; }

    /// <summary>When registration opens (the site's <c>checkInStartDeadline</c>). Only known from the detail call.</summary>
    public DateTimeOffset? RegistrationOpensAt { get; init; }

    /// <summary>When registration closes (the site's <c>checkInDeadline</c>).</summary>
    public DateTimeOffset? RegistrationClosesAt { get; init; }

    public DateTimeOffset? GameDate { get; init; }
    public DateTimeOffset? GameDateTo { get; init; }
}
