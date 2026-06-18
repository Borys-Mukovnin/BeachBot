using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeachBot.Api.Dtos;

/// <summary>
/// Reads/writes Meteor EJSON dates: <c>{"$date": &lt;unixMillis&gt;}</c>. Null is
/// passed through. Date fields are modelled as nullable because the API returns
/// null for unset dates.
/// </summary>
public sealed class EjsonDateConverter : JsonConverter<DateTimeOffset?>
{
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            // Tolerate a bare epoch-millis number just in case.
            case JsonTokenType.Number:
                return DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64());

            case JsonTokenType.StartObject:
                long? millis = null;
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        bool isDate = reader.ValueTextEquals("$date");
                        reader.Read();
                        if (isDate && reader.TokenType == JsonTokenType.Number)
                            millis = reader.GetInt64();
                        else
                            reader.Skip();
                    }
                }
                return millis is null ? null : DateTimeOffset.FromUnixTimeMilliseconds(millis.Value);

            default:
                throw new JsonException($"Unexpected token {reader.TokenType} for EJSON date.");
        }
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        writer.WriteNumber("$date", value.Value.ToUnixTimeMilliseconds());
        writer.WriteEndObject();
    }
}
