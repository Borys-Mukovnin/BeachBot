using System.Text.Json.Serialization;

namespace BeachBot.Api.Dtos;

/// <summary>Result of <c>tournaments.list</c>.</summary>
public sealed class TournamentListDto
{
    [JsonPropertyName("tournaments")] public List<TournamentDto> Tournaments { get; set; } = new();
    [JsonPropertyName("tournamentsCount")] public int TournamentsCount { get; set; }
}

/// <summary>
/// A tournament as returned by the list (light) and detail (full) calls. Fields
/// absent in the light form (e.g. <see cref="CheckInStartDeadline"/>) stay null.
/// </summary>
public sealed class TournamentDto
{
    [JsonPropertyName("_id")] public string Id { get; set; } = "";

    [JsonPropertyName("series")] public string? Series { get; set; }
    [JsonPropertyName("category")] public string? Category { get; set; }

    [JsonPropertyName("cityCache")] public string? CityCache { get; set; }
    [JsonPropertyName("resolvedName")] public string? ResolvedName { get; set; }

    [JsonPropertyName("teamSize")] public int TeamSize { get; set; }
    [JsonPropertyName("maxTeams")] public int MaxTeams { get; set; }
    [JsonPropertyName("teamCountCurrent")] public int TeamCountCurrent { get; set; }

    /// <summary>When registration opens. Detail call only.</summary>
    [JsonPropertyName("checkInStartDeadline")] public DateTimeOffset? CheckInStartDeadline { get; set; }

    /// <summary>When registration closes.</summary>
    [JsonPropertyName("checkInDeadline")] public DateTimeOffset? CheckInDeadline { get; set; }

    [JsonPropertyName("gameDate")] public DateTimeOffset? GameDate { get; set; }
    [JsonPropertyName("gameDateTo")] public DateTimeOffset? GameDateTo { get; set; }

    // Embedded name/gender lookups present in the light list form.
    [JsonPropertyName("seriesLink")] public SeriesRefDto? SeriesLink { get; set; }
    [JsonPropertyName("categoryLink")] public NamedRefDto? CategoryLink { get; set; }
}

/// <summary>Result of <c>tournament.getDetail</c> (only the parts we need).</summary>
public sealed class TournamentDetailDto
{
    [JsonPropertyName("tournament")] public TournamentDto? Tournament { get; set; }
    [JsonPropertyName("series")] public SeriesRefDto? Series { get; set; }
    [JsonPropertyName("category")] public NamedRefDto? Category { get; set; }
}

public sealed class SeriesRefDto
{
    [JsonPropertyName("_id")] public string? Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("teamConstraints")] public TeamConstraintsDto? TeamConstraints { get; set; }
}

public sealed class NamedRefDto
{
    [JsonPropertyName("_id")] public string? Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
}

public sealed class TeamConstraintsDto
{
    [JsonPropertyName("gender")] public string? Gender { get; set; }
}
