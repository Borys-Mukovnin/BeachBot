using System.Text.Json.Serialization;

namespace BeachBot.Api.Dtos;

/// <summary>Result of the <c>login</c> method (password or resume-token based).</summary>
public sealed class LoginResultDto
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("token")] public string? Token { get; set; }
    [JsonPropertyName("tokenExpires")] public DateTimeOffset? TokenExpires { get; set; }
    [JsonPropertyName("type")] public string? Type { get; set; }
}
