namespace LaundryAPI.Models;

public class ServiceOffering
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required ServiceUnit Unit { get; set; }
    public long UnitPriceVnd { get; set; }
    public bool IsActive { get; set; } = true;
}
