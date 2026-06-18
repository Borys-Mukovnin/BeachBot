using BeachBot.Core.Models;

namespace BeachBot.Application;

/// <summary>Filter options for the tournament list (matches the UI: city / series / category).</summary>
public sealed record TournamentFilterCriteria(
    string? City = null,
    string? SeriesId = null,
    string? CategoryId = null,
    bool OnlyRegistrationNotOpen = true);

/// <summary>
/// Pure, testable filtering for the tournament list. "Registration not yet open"
/// is the key rule from the activity diagram: keep tournaments where registration
/// is still in the future (detail known) or where nobody has registered yet (light list).
/// </summary>
public static class TournamentFilter
{
    public static IReadOnlyList<Tournament> Apply(
        IEnumerable<Tournament> tournaments, TournamentFilterCriteria criteria, DateTimeOffset now)
    {
        IEnumerable<Tournament> query = tournaments;

        if (criteria.OnlyRegistrationNotOpen)
            query = query.Where(t => IsRegistrationNotYetOpen(t, now));

        if (!string.IsNullOrWhiteSpace(criteria.City))
            query = query.Where(t => t.City.Contains(criteria.City.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(criteria.SeriesId))
            query = query.Where(t => string.Equals(t.SeriesId, criteria.SeriesId, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(criteria.CategoryId))
            query = query.Where(t => string.Equals(t.CategoryId, criteria.CategoryId, StringComparison.OrdinalIgnoreCase));

        return query.ToList();
    }

    public static bool IsRegistrationNotYetOpen(Tournament tournament, DateTimeOffset now)
    {
        if (tournament.RegistrationOpensAt is { } opensAt)
            return opensAt > now;
        return tournament.TeamCountCurrent == 0;
    }
}
