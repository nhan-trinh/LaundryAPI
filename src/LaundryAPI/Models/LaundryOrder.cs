using System.ComponentModel.DataAnnotations;

namespace LaundryAPI.Models;

public class LaundryOrder
{
    public const string Received = "Received";

    public int Id { get; set; }
    public string Code { get; private set; } = null!;
    public int CustomerId { get; init; }
    public DateTimeOffset? PromisedAt { get; init; }
    public string Status { get; set; } = Received;
    [Timestamp]
    public byte[] Version { get; private set; } = [];
    public List<OrderLine> Lines { get; } = [];
    public List<OrderStatusEvent> History { get; } = [];
}
