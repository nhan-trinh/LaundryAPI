using System.ComponentModel.DataAnnotations;
using LaundryAPI.Models;

namespace LaundryAPI.Dtos;

public record CreateStaffAccountRequest(
    [Required, MaxLength(Account.UsernameMaxLength)] string Username,
    [Required] string Password);

public record StaffAccountResponse(int Id, string Username);
