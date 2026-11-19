using System.ComponentModel.DataAnnotations;

namespace LaundryAPI.Validations;

public sealed class VietnamesePhoneAttribute() : RegularExpressionAttribute(@"(?:0|\+84)[1-9][0-9]{8,9}")
{
}
