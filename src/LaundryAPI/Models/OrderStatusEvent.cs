namespace LaundryAPI.Models;

public class OrderStatusEvent
{
    public int Id { get; set; }
    public int LaundryOrderId { get; init; }
    public required string Status { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public int AccountId { get; init; }
    public Account Account { get; private set; } = null!;
}
