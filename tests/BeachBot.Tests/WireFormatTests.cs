using System.Text.Json;
using BeachBot.Api.Dtos;
using BeachBot.Api.Mapping;
using Xunit;

namespace BeachBot.Tests;

/// <summary>
/// Validates the DTOs + EJSON converter against representative payloads taken
/// from api_schema.json, plus the DTO → Core mapping.
/// </summary>
public class WireFormatTests
{
    private static readonly JsonSerializerOptions Json = BeachBotJson.Options;

    [Fact]
    public void EjsonDate_Parses_DollarDate()
    {
        var dto = JsonSerializer.Deserialize<LoginResultDto>(
            """{"id":"Fqj2E4DQFJv2zzhnm","token":"abc","tokenExpires":{"$date":1789468270474},"type":"password"}""",
            Json)!;

        Assert.Equal("abc", dto.Token);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1789468270474), dto.TokenExpires);
    }

    [Fact]
    public void EjsonDate_Parses_Null()
    {
        var dto = JsonSerializer.Deserialize<TournamentDto>(
            """{"_id":"x","gameDate":null}""", Json)!;
        Assert.Null(dto.GameDate);
    }

    [Fact]
    public void TournamentsList_LightForm_DeserializesAndMaps()
    {
        const string json = """
        {
          "tournaments": [{
            "_id": "BZatkurzpjZZho2Hs",
            "series": "h",
            "category": "b",
            "cityCache": "Gütersloh",
            "teamSize": 2,
            "maxTeams": 9,
            "checkInDeadline": { "$date": 1780912800000 },
            "gameDate": { "$date": 1781690400000 },
            "gameDateTo": { "$date": 1781863200000 },
            "teamCountCurrent": 0,
            "seriesLink": { "_id": "h", "name": "Herren", "teamConstraints": { "gender": "male" } },
            "categoryLink": { "_id": "b", "name": "B" },
            "resolvedName": "Gütersloh"
          }],
          "tournamentsCount": 632
        }
        """;

        var dto = JsonSerializer.Deserialize<TournamentListDto>(json, Json)!;
        Assert.Equal(632, dto.TournamentsCount);
        var t = DtoMapper.MapTournament(Assert.Single(dto.Tournaments));

        Assert.Equal("BZatkurzpjZZho2Hs", t.Id);
        Assert.Equal("Gütersloh", t.Name);
        Assert.Equal("Gütersloh", t.City);
        Assert.Equal("h", t.SeriesId);
        Assert.Equal("Herren", t.SeriesName);
        Assert.Equal("male", t.Gender);
        Assert.Equal("b", t.CategoryId);
        Assert.Equal("B", t.CategoryName);
        Assert.Equal(2, t.TeamSize);
        Assert.Equal(0, t.TeamCountCurrent);
        Assert.Null(t.RegistrationOpensAt); // not present in the light form
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1781690400000), t.GameDate);
    }

    [Fact]
    public void TournamentDetail_MapsRegistrationOpensAtAndNames()
    {
        const string json = """
        {
          "tournament": {
            "_id": "BZatkurzpjZZho2Hs",
            "series": "h",
            "category": "b",
            "cityCache": "Gütersloh",
            "teamSize": 2,
            "maxTeams": 9,
            "teamCountCurrent": 0,
            "checkInStartDeadline": { "$date": 1780246800000 },
            "checkInDeadline": { "$date": 1780912800000 }
          },
          "series": { "_id": "h", "name": "Herren", "teamConstraints": { "gender": "male" } },
          "category": { "_id": "b", "name": "B" }
        }
        """;

        var dto = JsonSerializer.Deserialize<TournamentDetailDto>(json, Json)!;
        var t = DtoMapper.MapTournament(dto.Tournament!, dto.Series, dto.Category);

        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1780246800000), t.RegistrationOpensAt);
        Assert.Equal("Herren", t.SeriesName);
        Assert.Equal("male", t.Gender);
        Assert.Equal("B", t.CategoryName);
    }

    [Fact]
    public void SearchPlayers_DeserializesAndMaps()
    {
        const string json = """
        [{ "_id": "2sbZDT3KvcMLCGNKk",
           "profile": { "firstName": "Martin", "lastName": "Schmidt", "clubCache": "SSF Bonn 1905 e.V." },
           "score": 11.625 }]
        """;

        var dtos = JsonSerializer.Deserialize<List<SearchPlayerDto>>(json, Json)!;
        var player = DtoMapper.MapPlayer(Assert.Single(dtos));

        Assert.Equal("2sbZDT3KvcMLCGNKk", player.Id);
        Assert.Equal("Martin Schmidt", player.FullName);
        Assert.Equal("SSF Bonn 1905 e.V.", player.Club);
    }
}
