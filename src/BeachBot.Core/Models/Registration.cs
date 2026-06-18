namespace BeachBot.Core.Models;

public enum RegistrationStatus
{
    /// <summary>Waiting for <see cref="Registration.RegistrationOpensAt"/> to arrive.</summary>
    Scheduled,

    /// <summary>The registration request is being sent right now.</summary>
    Submitting,

    /// <summary>Registration succeeded.</summary>
    Succeeded,

    /// <summary>Registration failed (see <see cref="Registration.ResultMessage"/>).</summary>
    Failed,

    /// <summary>The user cancelled before it fired.</summary>
    Cancelled,
}

/// <summary>
/// A registration the app will perform (or has performed) on the user's behalf.
/// One object covers both the "scheduled" and "past" states shown on the main menu.
/// </summary>
public sealed class Registration
{
    public required string Id { get; init; }

    public required string TournamentId { get; init; }
    public required string TournamentName { get; init; }

    public required string PartnerId { get; init; }
    public required string PartnerName { get; init; }

    /// <summary>When the registration request should fire (the tournament's registration-open time).</summary>
    public required DateTimeOffset RegistrationOpensAt { get; init; }

    public RegistrationStatus Status { get; set; } = RegistrationStatus.Scheduled;

    /// <summary>Human-readable result/error message once the registration has run.</summary>
    public string? ResultMessage { get; set; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>True while the registration is still waiting to fire.</summary>
    public bool IsPending => Status is RegistrationStatus.Scheduled or RegistrationStatus.Submitting;
}
