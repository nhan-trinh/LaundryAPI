using System.Text.Json;
using System.Text.Json.Serialization;
using LaundryAPI.Models;

namespace LaundryAPI.Dtos;

public class ServiceUnitJsonConverter : JsonConverter<ServiceUnit>
{
    public override ServiceUnit Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected a service unit string.");
        return reader.GetString() switch
        {
            "kg" => ServiceUnit.Kg,
            "piece" => ServiceUnit.Piece,
            _ => throw new JsonException("Expected a service unit of 'kg' or 'piece'.")
        };
    }

    public override void Write(Utf8JsonWriter writer, ServiceUnit value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value switch
        {
            ServiceUnit.Kg => "kg",
            ServiceUnit.Piece => "piece",
            _ => throw new JsonException("Unknown service unit.")
        });
}
