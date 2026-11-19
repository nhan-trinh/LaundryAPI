using System.ComponentModel.DataAnnotations;
using LaundryAPI.Dtos;
using LaundryAPI.Models;
using LaundryAPI.Services;
using LaundryAPI.Validations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryAPI.Controllers;

[ApiController]
[Route("api/v1/customers")]
[Authorize(Roles = Account.StaffRole + "," + Account.ManagerRole)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public class CustomersController(ICustomerService customers) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustomerResponse>> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var (customer, created) = await customers.GetOrCreateAsync(request, cancellationToken);
        if (!created)
            return Ok(customer);

        return CreatedAtAction(nameof(Find), new { phoneNumber = customer.PhoneNumber }, customer);
    }

    [HttpGet]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponse>> Find([FromQuery, Required, VietnamesePhone] string phoneNumber,
        CancellationToken cancellationToken)
    {
        var customer = await customers.FindAsync(phoneNumber, cancellationToken);
        if (customer is null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Customer not found.");

        return Ok(customer);
    }
}
