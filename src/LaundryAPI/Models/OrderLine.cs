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
    public decimal Quantity { get; init; }
    public string? Note { get; init; }
    public decimal AmountVnd
    {
        get
        {
            var wholeUnits = decimal.Truncate(Quantity);
            var fractionalVnd = (Quantity - wholeUnits) * UnitPriceVnd;
            return wholeUnits * UnitPriceVnd + decimal.Round(fractionalVnd, 0, MidpointRounding.AwayFromZero);
        }
    }
}
