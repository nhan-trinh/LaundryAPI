using System.ComponentModel.DataAnnotations;
using LaundryAPI.Validations;

namespace LaundryAPI.Dtos;

public record CreateCustomerRequest([Required] string Name, [Required, VietnamesePhone] string PhoneNumber);
public record CustomerResponse(int Id, string Name, string PhoneNumber);
