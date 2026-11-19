using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LaundryAPI.Models;

public class OrderPayment
{
    public const string Cash = "cash";
    public const string BankTransfer = "bankTransfer";

    [MaxLength(12)]
    public required string Method { get; init; }
    [Precision(29, 0)]
    public decimal AmountVnd { get; init; }
}
