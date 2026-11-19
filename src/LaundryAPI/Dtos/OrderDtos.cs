using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using LaundryAPI.Models;
using LaundryAPI.Validations;

namespace LaundryAPI.Dtos;

public record CreateOrderRequest([Range(1, int.MaxValue)] int CustomerId, [Required, MinLength(1)] List<CreateOrderLine> Lines,
    DateTimeOffset? PromisedAt);
public record CreateOrderLine(int ServiceId,
    [property: JsonConverter(typeof(OrderQuantityJsonConverter))]
    [OrderQuantityRange]
    decimal Quantity, string? Note);
public record ChangeOrderQuantityRequest([Required, MinLength(8), MaxLength(8)] byte[] Version,
    [property: JsonConverter(typeof(OrderQuantityJsonConverter))]
    [OrderQuantityRange]
    decimal Quantity);

public record CancelOrderRequest([Required, MinLength(8), MaxLength(8)] byte[] Version,
    [Required] string CancellationReason);

public record OrderProgressRequest([Required, MinLength(8), MaxLength(8)] byte[] Version);

public record OrderResponse(string Code, int CustomerId, DateTimeOffset? PromisedAt, string Status,
    byte[] Version, IReadOnlyList<OrderLineResponse> Lines, decimal TotalVnd, IReadOnlyList<OrderEventResponse> History,
    string? CancellationReason, DateTimeOffset? CancelledAt, int? CancelledBy);
public record OrderLineResponse(int Id, int ServiceId, string ServiceName, ServiceUnit Unit, long UnitPriceVnd,
    decimal Quantity, string? Note, decimal AmountVnd);
public record OrderEventResponse(string Status, DateTimeOffset OccurredAt, int AccountId, string Username);
