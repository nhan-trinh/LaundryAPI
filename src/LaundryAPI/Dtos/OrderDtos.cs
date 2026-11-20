using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using LaundryAPI.Models;
using LaundryAPI.Validations;

namespace LaundryAPI.Dtos;

public record OrderSearchRequest
{
    public string? Code { get; init; }
    [VietnamesePhone] public string? PhoneNumber { get; init; }
    [AllowedValues(null, LaundryOrder.Received, LaundryOrder.Processing, LaundryOrder.Ready,
        LaundryOrder.Returned, LaundryOrder.Cancelled)]
    public string? Status { get; init; }
    public DateOnly? ReceivedDate { get; init; }
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
}

public record OrderSummaryResponse(string Code, int CustomerId, string Status, DateTimeOffset ReceivedAt);

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
public record HandoverOrderRequest([Required, MinLength(8), MaxLength(8)] byte[] Version,
    [Required, AllowedValues(OrderPayment.Cash, OrderPayment.BankTransfer)] string PaymentMethod);

public record OrderResponse(string Code, int CustomerId, DateTimeOffset? PromisedAt, string Status,
    byte[] Version, IReadOnlyList<OrderLineResponse> Lines, decimal TotalVnd, IReadOnlyList<OrderEventResponse> History,
    string? CancellationReason, DateTimeOffset? CancelledAt, int? CancelledBy, OrderPaymentResponse? Payment);
public record OrderPaymentResponse(string Method, decimal AmountVnd);
public record OrderLineResponse(int Id, int ServiceId, string ServiceName, ServiceUnit Unit, long UnitPriceVnd,
    decimal Quantity, string? Note, decimal AmountVnd);
public record OrderEventResponse(string Status, DateTimeOffset OccurredAt, int AccountId, string Username);
