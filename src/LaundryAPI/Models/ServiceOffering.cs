using System.ComponentModel.DataAnnotations;

namespace LaundryAPI.Models;

public class ServiceOffering
{
    public int Id { get; set; }
    public required string Name { get; set; }
    [MaxLength(5)]
    public required string Unit { get; set; }
    public long UnitPriceVnd { get; set; }
    public bool IsActive { get; set; } = true;
}
