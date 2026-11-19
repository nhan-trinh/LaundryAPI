using System.ComponentModel.DataAnnotations;

namespace LaundryAPI.Dtos;

public class ServiceOfferingRequest
{
    [Required]
    public required string Name { get; init; }
    [Required, RegularExpression("^(kg|piece)$")]
    public required string Unit { get; init; }
    [Range(typeof(long), "0", "9223372036854775807")]
    public required long UnitPriceVnd { get; init; }
}

public record ServiceOfferingResponse(int Id, string Name, string Unit, long UnitPriceVnd);
