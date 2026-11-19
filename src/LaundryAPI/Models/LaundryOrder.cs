using System.ComponentModel.DataAnnotations;

namespace LaundryAPI.Models;

public enum OrderChangeResult { Saved, NotFound, Conflict }

public class LaundryOrder
{
    public const string Received = "Received";
    public const string Processing = "Processing";
    public const string Ready = "Ready";
    public const string Cancelled = "Cancelled";

    public int Id { get; set; }
    public string Code { get; private set; } = null!;
    public int CustomerId { get; init; }
    public DateTimeOffset? PromisedAt { get; init; }
    public string Status { get; private set; } = Received;
    public string? CancellationReason { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public int? CancelledBy { get; set; }
    [Timestamp]
    public byte[] Version { get; private set; } = [];
    public List<OrderLine> Lines { get; } = [];
    public List<OrderStatusEvent> History { get; } = [];

    public void Cancel(string reason, int accountId, DateTimeOffset now)
    {
        CancellationReason = reason;
        CancelledAt = now;
        CancelledBy = accountId;
        ChangeStatus(Cancelled, accountId, now);
    }

    public void StartProcessing(int accountId) => ChangeStatus(Processing, accountId, DateTimeOffset.UtcNow);

    public void MarkReady(int accountId) => ChangeStatus(Ready, accountId, DateTimeOffset.UtcNow);

    private void ChangeStatus(string status, int accountId, DateTimeOffset now)
    {
        Status = status;
        History.Add(new OrderStatusEvent { Status = status, OccurredAt = now, AccountId = accountId });
    }
}
