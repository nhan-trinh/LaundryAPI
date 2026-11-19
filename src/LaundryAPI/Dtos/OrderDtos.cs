using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using LaundryAPI.Models;

namespace LaundryAPI.Dtos;

public record CreateOrderRequest([Range(1, int.MaxValue)] int CustomerId, [Required, MinLength(1)] List<CreateOrderLine> Lines,
    DateTimeOffset? PromisedAt);
public record CreateOrderLine(int ServiceId,
    [property: JsonConverter(typeof(OrderQuantityJsonConverter))]
    [Range(typeof(decimal), "0.01", "99999999999999999999999999.99", ParseLimitsInInvariantCulture = true)]
    decimal Quantity, string? Note);

public record OrderResponse(string Code, int CustomerId, DateTimeOffset? PromisedAt, string Status,
    byte[] Version, IReadOnlyList<OrderLineResponse> Lines, decimal TotalVnd, IReadOnlyList<OrderEventResponse> History);
public record OrderLineResponse(int Id, int ServiceId, string ServiceName, ServiceUnit Unit, long UnitPriceVnd,
    decimal Quantity, string? Note, decimal AmountVnd);
public record OrderEventResponse(string Status, DateTimeOffset OccurredAt, int AccountId, string Username);
