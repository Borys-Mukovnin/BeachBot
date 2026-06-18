using BeachBot.Core.Models;

namespace BeachBot.Core.Abstractions;

/// <summary>
/// The use-case surface a frontend binds to. This is the seam that keeps the
/// backend decoupled: any UI (desktop today, web/mobile later) drives the whole
/// flow — authenticate, browse tournaments, pick a partner, schedule — through
/// this single interface.
/// </summary>
public interface IRegistrationService
{
    bool IsAuthenticated { get; }
    string? CurrentUserEmail { get; }

    /// <summary>Tries to silently re-authenticate from a saved "remember me" token. Returns true on success.</summary>
    Task<bool> TryResumeAsync(CancellationToken cancellationToken = default);

    /// <summary>Logs in with email + password. When <paramref name="rememberMe"/> is set, the resume token is stored for next time.</summary>
    Task LoginAsync(string email, string password, bool rememberMe, CancellationToken cancellationToken = default);

    /// <summary>Logs out and forgets any saved credentials.</summary>
    Task LogoutAsync(CancellationToken cancellationToken = default);

    /// <summary>All upcoming tournaments. Filtering (city/category/series, registration-not-open) is applied by the caller.</summary>
    Task<IReadOnlyList<Tournament>> GetUpcomingTournamentsAsync(CancellationToken cancellationToken = default);

    /// <summary>Full detail for one tournament, including when registration opens.</summary>
    Task<Tournament> GetTournamentDetailAsync(string tournamentId, CancellationToken cancellationToken = default);

    /// <summary>Searches for a partner by name.</summary>
    Task<IReadOnlyList<Player>> SearchPlayersAsync(string query, string? seriesId = null, CancellationToken cancellationToken = default);

    /// <summary>Partners used in past registrations, offered as quick picks.</summary>
    IReadOnlyList<Player> GetSavedPartners();

    /// <summary>Removes a partner from the saved quick-pick list.</summary>
    void RemoveSavedPartner(string partnerId);

    /// <summary>Schedules a registration to fire when the tournament opens. Persists it and returns the created record.</summary>
    Task<Registration> ScheduleRegistrationAsync(Tournament tournament, Player partner, CancellationToken cancellationToken = default);

    /// <summary>Cancels a still-pending scheduled registration.</summary>
    Task CancelRegistrationAsync(string registrationId, CancellationToken cancellationToken = default);

    /// <summary>Registrations still waiting to fire.</summary>
    IReadOnlyList<Registration> GetScheduledRegistrations();

    /// <summary>Registrations that have already run (succeeded, failed, or cancelled).</summary>
    IReadOnlyList<Registration> GetPastRegistrations();

    /// <summary>Raised when the registration lists change (e.g. a scheduled registration fired).</summary>
    event EventHandler? RegistrationsChanged;
}
