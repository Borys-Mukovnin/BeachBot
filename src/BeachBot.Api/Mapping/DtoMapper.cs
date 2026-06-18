using BeachBot.Api.Dtos;
using BeachBot.Core.Models;

namespace BeachBot.Api.Mapping;

/// <summary>Maps wire DTOs onto the Core domain models.</summary>
public static class DtoMapper
{
    /// <summary>
    /// Builds a <see cref="Tournament"/>. The detail call supplies resolved
    /// <paramref name="series"/>/<paramref name="category"/> objects; the list
    /// call instead embeds them on the tournament itself, so we fall back to those.
    /// </summary>
    public static Tournament MapTournament(TournamentDto dto, SeriesRefDto? series = null, NamedRefDto? category = null)
    {
        return new Tournament
        {
            Id = dto.Id,
            Name = FirstNonEmpty(dto.ResolvedName, dto.CityCache) ?? dto.Id,
            City = FirstNonEmpty(dto.CityCache, dto.ResolvedName) ?? "",
            SeriesId = dto.Series ?? series?.Id ?? dto.SeriesLink?.Id,
            SeriesName = series?.Name ?? dto.SeriesLink?.Name,
            CategoryId = dto.Category ?? category?.Id ?? dto.CategoryLink?.Id,
            CategoryName = category?.Name ?? dto.CategoryLink?.Name,
            Gender = series?.TeamConstraints?.Gender ?? dto.SeriesLink?.TeamConstraints?.Gender,
            TeamSize = dto.TeamSize,
            MaxTeams = dto.MaxTeams,
            TeamCountCurrent = dto.TeamCountCurrent,
            RegistrationOpensAt = dto.CheckInStartDeadline,
            RegistrationClosesAt = dto.CheckInDeadline,
            GameDate = dto.GameDate,
            GameDateTo = dto.GameDateTo,
        };
    }

    public static Player MapPlayer(SearchPlayerDto dto)
    {
        return new Player
        {
            Id = dto.Id,
            FirstName = dto.Profile?.FirstName ?? "",
            LastName = dto.Profile?.LastName ?? "",
            Club = dto.Profile?.ClubCache,
        };
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}
