using System.ComponentModel.DataAnnotations;
using LaundryAPI.Models;

namespace LaundryAPI.Dtos;

public class ServiceOfferingRequest
{
    [Required]
    public required string Name { get; init; }
    public required ServiceUnit Unit { get; init; }
    [Range(typeof(long), "0", "9223372036854775807")]
    public required long UnitPriceVnd { get; init; }
}

public record ServiceOfferingResponse(int Id, string Name, ServiceUnit Unit, long UnitPriceVnd);
