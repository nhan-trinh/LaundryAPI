using System.Text.Json;
using System.Text.Json.Serialization;

namespace LaundryAPI.Dtos;

public class OrderQuantityJsonConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number)
            throw new JsonException("Expected a numeric quantity.");

        var number = JsonElement.ParseValue(ref reader);
        // TryGetDecimal can round a fractional piece to a whole number before unit validation.
        if (!number.TryGetDecimal(out var quantity) ||
            !JsonElement.DeepEquals(number, JsonSerializer.SerializeToElement(quantity)))
            throw new JsonException("Quantity exceeds the supported precision.");

        return quantity;
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value);
}
