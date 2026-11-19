using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LaundryAPI.Models;

[Index(nameof(PhoneNumber), IsUnique = true)]
public class Customer
{
    public int Id { get; set; }
    public required string Name { get; set; }
    [MaxLength(13)]
    public required string PhoneNumber { get; set; }
}
