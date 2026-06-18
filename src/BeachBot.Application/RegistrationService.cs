using BeachBot.Api.Auth;
using BeachBot.Api.Ddp;
using BeachBot.Application.Storage;
using BeachBot.Core.Abstractions;
using BeachBot.Core.Models;

namespace BeachBot.Application;

/// <summary>
/// The use-case orchestrator behind <see cref="IRegistrationService"/>. It owns the
/// session (connect + authenticate, reconnecting as needed), exposes the browse
/// operations, and drives the scheduler so each registration fires the moment the
/// tournament opens — re-authenticating from the saved token if the app had to wait.
/// </summary>
public sealed class RegistrationService : IRegistrationService, IAsyncDisposable
{
    private readonly IDdpConnection _connection;
    private readonly IAuthenticator _authenticator;
    private readonly ITournamentApi _api;
    private readonly IClock _clock;
    private readonly RegistrationScheduler _scheduler;
    private readonly ICredentialStore _credentials;
    private readonly IRegistrationStore _registrations;
    private readonly IPlayerProfileStore _partners;

    private readonly SemaphoreSlim _sessionLock = new(1, 1);
    private string? _email;
    private AuthToken? _token;
    private bool _authenticated;

    public RegistrationService(
        IDdpConnection connection,
        IAuthenticator authenticator,
        ITournamentApi api,
        IClock clock,
        RegistrationScheduler scheduler,
        ICredentialStore credentials,
        IRegistrationStore registrations,
        IPlayerProfileStore partners)
    {
        _connection = connection;
        _authenticator = authenticator;
        _api = api;
        _clock = clock;
        _scheduler = scheduler;
        _credentials = credentials;
        _registrations = registrations;
        _partners = partners;

        RescheduleSavedRegistrations();
    }

    public bool IsAuthenticated => _authenticated;
    public string? CurrentUserEmail => _email;

    public event EventHandler? RegistrationsChanged;

    // ---- Authentication -------------------------------------------------

    public async Task<bool> TryResumeAsync(CancellationToken cancellationToken = default)
    {
        var saved = _credentials.Load();
        if (saved is null || !saved.Token.IsValid(_clock.UtcNow))
            return false;

        try
        {
            await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
            var token = await _authenticator.ResumeAsync(saved.Token.Token, cancellationToken).ConfigureAwait(false);
            _email = saved.Email;
            _token = token;
            _authenticated = true;
            _credentials.Save(new StoredCredentials(saved.Email, token));
            return true;
        }
        catch
        {
            _credentials.Clear();
            return false;
        }
    }

    public async Task LoginAsync(string email, string password, bool rememberMe, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        var token = await _authenticator.LoginAsync(email, password, cancellationToken).ConfigureAwait(false);
        _email = email;
        _token = token;
        _authenticated = true;

        if (rememberMe)
            _credentials.Save(new StoredCredentials(email, token));
        else
            _credentials.Clear();
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        _authenticated = false;
        _token = null;
        _email = null;
        _credentials.Clear();
        return Task.CompletedTask;
    }

    // ---- Browsing -------------------------------------------------------

    public Task<IReadOnlyList<Tournament>> GetUpcomingTournamentsAsync(CancellationToken cancellationToken = default)
        => RunWithSessionAsync(ct => _api.ListUpcomingTournamentsAsync(200, ct), cancellationToken);

    public Task<Tournament> GetTournamentDetailAsync(string tournamentId, CancellationToken cancellationToken = default)
        => RunWithSessionAsync(ct => _api.GetTournamentDetailAsync(tournamentId, ct), cancellationToken);

    public Task<IReadOnlyList<Player>> SearchPlayersAsync(string query, string? seriesId = null, CancellationToken cancellationToken = default)
        => RunWithSessionAsync(ct => _api.SearchPlayersAsync(query, seriesId, ct), cancellationToken);

    public IReadOnlyList<Player> GetSavedPartners() => _partners.GetPartners();

    public void RemoveSavedPartner(string partnerId) => _partners.RemovePartner(partnerId);

    // ---- Scheduling -----------------------------------------------------

    public async Task<Registration> ScheduleRegistrationAsync(Tournament tournament, Player partner, CancellationToken cancellationToken = default)
    {
        // We need the exact open time; fetch detail if the list form didn't include it.
        var opensAt = tournament.RegistrationOpensAt;
        if (opensAt is null)
        {
            var detail = await GetTournamentDetailAsync(tournament.Id, cancellationToken).ConfigureAwait(false);
            opensAt = detail.RegistrationOpensAt;
        }

        var registration = new Registration
        {
            Id = Guid.NewGuid().ToString("N"),
            TournamentId = tournament.Id,
            TournamentName = tournament.Name,
            PartnerId = partner.Id,
            PartnerName = partner.FullName,
            RegistrationOpensAt = opensAt ?? _clock.UtcNow, // already open → fire now
            CreatedAt = _clock.UtcNow,
            Status = RegistrationStatus.Scheduled,
        };

        _registrations.Upsert(registration);
        _partners.AddPartner(partner);
        _scheduler.Schedule(registration.Id, registration.RegistrationOpensAt, ct => FireAsync(registration, ct));
        RaiseChanged();
        return registration;
    }

    public Task CancelRegistrationAsync(string registrationId, CancellationToken cancellationToken = default)
    {
        _scheduler.Cancel(registrationId);
        var registration = _registrations.GetAll().FirstOrDefault(r => r.Id == registrationId);
        if (registration is { IsPending: true })
        {
            registration.Status = RegistrationStatus.Cancelled;
            registration.CompletedAt = _clock.UtcNow;
            registration.ResultMessage = "Cancelled.";
            _registrations.Upsert(registration);
            RaiseChanged();
        }
        return Task.CompletedTask;
    }

    public IReadOnlyList<Registration> GetScheduledRegistrations()
        => _registrations.GetAll().Where(r => r.IsPending).OrderBy(r => r.RegistrationOpensAt).ToList();

    public IReadOnlyList<Registration> GetPastRegistrations()
        => _registrations.GetAll().Where(r => !r.IsPending).OrderByDescending(r => r.CompletedAt ?? r.CreatedAt).ToList();

    // ---- Internals ------------------------------------------------------

    /// <summary>The scheduler callback: authenticate (resuming if needed) and send the registration.</summary>
    private async Task FireAsync(Registration registration, CancellationToken cancellationToken)
    {
        registration.Status = RegistrationStatus.Submitting;
        _registrations.Upsert(registration);
        RaiseChanged();

        try
        {
            await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
            await _api.RegisterAsync(registration.TournamentId, registration.PartnerId, cancellationToken).ConfigureAwait(false);
            registration.Status = RegistrationStatus.Succeeded;
            registration.ResultMessage = "Registered successfully.";
        }
        catch (DdpException ex)
        {
            registration.Status = RegistrationStatus.Failed;
            registration.ResultMessage = ex.Reason ?? ex.Message;
        }
        catch (Exception ex)
        {
            registration.Status = RegistrationStatus.Failed;
            registration.ResultMessage = ex.Message;
        }
        finally
        {
            registration.CompletedAt = _clock.UtcNow;
            _registrations.Upsert(registration);
            RaiseChanged();
        }
    }

    private void RescheduleSavedRegistrations()
    {
        foreach (var registration in _registrations.GetAll().Where(r => r.IsPending))
            _scheduler.Schedule(registration.Id, registration.RegistrationOpensAt, ct => FireAsync(registration, ct));
    }

    private async Task<T> RunWithSessionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await action(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsConnectionError(ex))
        {
            // The socket dropped; reconnect, re-authenticate, and try once more.
            _authenticated = false;
            await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
            return await action(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_connection.IsConnected)
            return;
        await _connection.ConnectAsync(cancellationToken).ConfigureAwait(false);
        _authenticated = false; // a fresh socket has no authenticated session
    }

    private async Task EnsureSessionAsync(CancellationToken cancellationToken)
    {
        if (_connection.IsConnected && _authenticated)
            return;

        await _sessionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
            if (_authenticated)
                return;

            // Re-authenticate using the in-memory token, falling back to the saved one.
            var resumeToken = (_token is not null && _token.IsValid(_clock.UtcNow)) ? _token.Token : null;
            string? email = _email;
            if (resumeToken is null)
            {
                var saved = _credentials.Load();
                if (saved is not null && saved.Token.IsValid(_clock.UtcNow))
                {
                    resumeToken = saved.Token.Token;
                    email = saved.Email;
                }
            }

            if (resumeToken is null)
                throw new InvalidOperationException("Not signed in. Please log in first.");

            var token = await _authenticator.ResumeAsync(resumeToken, cancellationToken).ConfigureAwait(false);
            _email = email;
            _token = token;
            _authenticated = true;
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    private static bool IsConnectionError(Exception ex)
        => ex is System.Net.WebSockets.WebSocketException or IOException
           || (ex is InvalidOperationException && ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase));

    private void RaiseChanged() => RegistrationsChanged?.Invoke(this, EventArgs.Empty);

    public async ValueTask DisposeAsync()
    {
        await _scheduler.DisposeAsync().ConfigureAwait(false);
        await _connection.DisposeAsync().ConfigureAwait(false);
        _sessionLock.Dispose();
    }
}
