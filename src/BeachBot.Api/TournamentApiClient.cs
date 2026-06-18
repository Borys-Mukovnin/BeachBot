using BeachBot.Api.Ddp;
using BeachBot.Api.Dtos;
using BeachBot.Api.Mapping;
using BeachBot.Core.Abstractions;
using BeachBot.Core.Models;

namespace BeachBot.Api;

/// <summary>
/// Typed facade over <see cref="IDdpConnection"/> that implements
/// <see cref="ITournamentApi"/>. Each method maps directly to one DDP call.
/// </summary>
public sealed class TournamentApiClient : ITournamentApi
{
    private readonly IDdpConnection _connection;

    public TournamentApiClient(IDdpConnection connection) => _connection = connection;

    public async Task<IReadOnlyList<Tournament>> ListUpcomingTournamentsAsync(int pageSize = 200, CancellationToken cancellationToken = default)
    {
        if (pageSize <= 0)
            pageSize = 200;

        var all = new List<Tournament>();
        int skip = 0;
        int total = int.MaxValue;

        // Page until we've collected the whole set (the server reports tournamentsCount).
        while (skip < total)
        {
            var @params = new object?[] { new { view = "upcoming", limit = pageSize, skip } };
            var page = await _connection.CallAsync<TournamentListDto>("tournaments.list", @params, cancellationToken).ConfigureAwait(false);
            if (page is null || page.Tournaments.Count == 0)
                break;

            total = page.TournamentsCount;
            all.AddRange(page.Tournaments.Select(t => DtoMapper.MapTournament(t)));
            skip += page.Tournaments.Count;
        }

        return all;
    }

    public async Task<Tournament> GetTournamentDetailAsync(string tournamentId, CancellationToken cancellationToken = default)
    {
        var result = await _connection.CallAsync<TournamentDetailDto>("tournament.getDetail", new object?[] { tournamentId }, cancellationToken)
            .ConfigureAwait(false);
        if (result?.Tournament is null)
            throw new DdpException($"No tournament detail returned for '{tournamentId}'.");
        return DtoMapper.MapTournament(result.Tournament, result.Series, result.Category);
    }

    public async Task<IReadOnlyList<Player>> SearchPlayersAsync(string searchText, string? seriesId = null, CancellationToken cancellationToken = default)
    {
        var @params = new object?[] { searchText, new { showUnverified = false, series = seriesId } };
        var result = await _connection.CallAsync<List<SearchPlayerDto>>("users.searchPlayers", @params, cancellationToken).ConfigureAwait(false);
        if (result is null)
            return Array.Empty<Player>();
        return result.Select(DtoMapper.MapPlayer).ToList();
    }

    public async Task RegisterAsync(string tournamentId, string partnerId, CancellationToken cancellationToken = default)
    {
        // Success is "no DDP error reply"; a failure surfaces as a DdpException.
        await _connection.CallAsync("tournament.registration", new object?[] { tournamentId, partnerId }, cancellationToken)
            .ConfigureAwait(false);
    }
}
