using System.ComponentModel.DataAnnotations;

namespace LaundryAPI.Validations;

public sealed class OrderQuantityRangeAttribute : RangeAttribute
{
    public OrderQuantityRangeAttribute() : base(typeof(decimal), "0.01", "99999999999999999999999999.99")
    {
        ParseLimitsInInvariantCulture = true;
    }
}
