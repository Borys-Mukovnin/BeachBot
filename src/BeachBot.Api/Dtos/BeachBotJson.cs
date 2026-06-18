using System.Text.Json;

namespace BeachBot.Api.Dtos;

/// <summary>Shared serializer options for the DDP wire format (EJSON dates, lenient reads).</summary>
public static class BeachBotJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
            // Meteor's check() treats optional method params as "omit", not "null":
            // sending {"userId":null} fails with "Match failed". Drop null fields instead.
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };
        options.Converters.Add(new EjsonDateConverter());
        return options;
    }
}
