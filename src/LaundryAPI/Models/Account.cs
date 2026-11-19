using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LaundryAPI.Models;

[Index(nameof(Username), IsUnique = true)]
public class Account
{
    public const int UsernameMaxLength = 128;
    public const string ManagerRole = "Manager";

    public int Id { get; set; }
    [MaxLength(UsernameMaxLength)]
    public required string Username { get; set; }
    [MaxLength(32)]
    public required string Role { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
}
