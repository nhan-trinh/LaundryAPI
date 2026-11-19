using System.ComponentModel.DataAnnotations;
using LaundryAPI.Models;

namespace LaundryAPI.Dtos;

public record LoginRequest(
    [Required, MaxLength(Account.UsernameMaxLength)] string Username,
    [Required] string Password);

public record LoginResponse(string AccessToken);
public record CurrentAccount(string Username, string Role);
