using BeachBot.Application;
using BeachBot.Core.Models;
using Xunit;

namespace BeachBot.Tests;

public class TournamentFilterTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);

    private static Tournament Make(
        string id, string city, string? series, string? category,
        int teamCount = 0, DateTimeOffset? opensAt = null) => new()
    {
        Id = id,
        Name = city,
        City = city,
        SeriesId = series,
        CategoryId = category,
        TeamCountCurrent = teamCount,
        RegistrationOpensAt = opensAt,
    };

    [Fact]
    public void NotYetOpen_TrueWhenNoTeamsRegisteredYet()
        => Assert.True(TournamentFilter.IsRegistrationNotYetOpen(Make("a", "Köln", "h", "b", teamCount: 0), Now));

    [Fact]
    public void NotYetOpen_FalseWhenTeamsAlreadyRegistered()
        => Assert.False(TournamentFilter.IsRegistrationNotYetOpen(Make("a", "Köln", "h", "b", teamCount: 5), Now));

    [Fact]
    public void NotYetOpen_UsesOpenTimeWhenKnown()
    {
        Assert.True(TournamentFilter.IsRegistrationNotYetOpen(Make("a", "Köln", "h", "b", teamCount: 9, opensAt: Now.AddDays(1)), Now));
        Assert.False(TournamentFilter.IsRegistrationNotYetOpen(Make("a", "Köln", "h", "b", teamCount: 0, opensAt: Now.AddDays(-1)), Now));
    }

    [Fact]
    public void Apply_FiltersByCityCaseInsensitiveSubstring()
    {
        var list = new[] { Make("a", "Köln", "h", "b"), Make("b", "Münster", "h", "b") };
        var result = TournamentFilter.Apply(list, new TournamentFilterCriteria(City: "köln"), Now);
        Assert.Equal("a", Assert.Single(result).Id);
    }

    [Fact]
    public void Apply_FiltersBySeriesAndCategory()
    {
        var list = new[]
        {
            Make("a", "Köln", "h", "b"),
            Make("b", "Köln", "d", "b"),
            Make("c", "Köln", "h", "a"),
        };

        var result = TournamentFilter.Apply(list, new TournamentFilterCriteria(SeriesId: "h", CategoryId: "b"), Now);
        Assert.Equal("a", Assert.Single(result).Id);
    }

    [Fact]
    public void Apply_ExcludesAlreadyOpenTournaments_ByDefault()
    {
        var list = new[]
        {
            Make("open", "Köln", "h", "b", teamCount: 5),
            Make("notopen", "Köln", "h", "b", teamCount: 0),
        };

        var result = TournamentFilter.Apply(list, new TournamentFilterCriteria(), Now);
        Assert.Equal("notopen", Assert.Single(result).Id);
    }
}
