using System.Text.Json.Serialization;

namespace BeachBot.Api.Dtos;

/// <summary>One hit from <c>users.searchPlayers</c> (ranked by fuzzy <see cref="Score"/>).</summary>
public sealed class SearchPlayerDto
{
    [JsonPropertyName("_id")] public string Id { get; set; } = "";
    [JsonPropertyName("profile")] public PlayerProfileDto? Profile { get; set; }
    [JsonPropertyName("score")] public double Score { get; set; }
}

public sealed class PlayerProfileDto
{
    [JsonPropertyName("firstName")] public string? FirstName { get; set; }
    [JsonPropertyName("lastName")] public string? LastName { get; set; }
    [JsonPropertyName("clubCache")] public string? ClubCache { get; set; }
}
