using BeachBot.Core.Models;

namespace BeachBot.Core.Abstractions;

/// <summary>
/// Typed access to the beachvolleyball.de backend. Implementations talk the raw
/// DDP/WebSocket protocol; the rest of the app only sees domain models.
/// </summary>
public interface ITournamentApi
{
    /// <summary>Lists all upcoming tournaments (light form), paging through the full result set.</summary>
    Task<IReadOnlyList<Tournament>> ListUpcomingTournamentsAsync(int pageSize = 200, CancellationToken cancellationToken = default);

    /// <summary>Fetches the full detail for one tournament, including when registration opens.</summary>
    Task<Tournament> GetTournamentDetailAsync(string tournamentId, CancellationToken cancellationToken = default);

    /// <summary>Fuzzy-searches players by name to pick a registration partner.</summary>
    Task<IReadOnlyList<Player>> SearchPlayersAsync(string searchText, string? seriesId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers the logged-in user together with <paramref name="partnerId"/> for the tournament.
    /// Throws on failure (e.g. registration not open, already registered, missing license).
    /// </summary>
    Task RegisterAsync(string tournamentId, string partnerId, CancellationToken cancellationToken = default);
}
