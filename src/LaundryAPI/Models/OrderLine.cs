using Microsoft.EntityFrameworkCore;

namespace LaundryAPI.Models;

public class OrderLine
{
    public int Id { get; set; }
    public int LaundryOrderId { get; init; }
    public int Position { get; init; }
    public int ServiceId { get; init; }
    public required string ServiceName { get; init; }
    public ServiceUnit Unit { get; init; }
    public long UnitPriceVnd { get; init; }
    [Precision(28, 2)]
    public decimal Quantity { get; set; }
    public string? Note { get; init; }
}
