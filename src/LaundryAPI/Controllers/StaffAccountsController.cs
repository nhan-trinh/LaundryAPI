using LaundryAPI.Dtos;
using LaundryAPI.Models;
using LaundryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryAPI.Controllers;

[ApiController]
[Route("api/v1/staff-accounts")]
[Authorize(Roles = Account.ManagerRole)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public class StaffAccountsController(StaffAccountService staffAccounts) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<StaffAccountResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StaffAccountResponse>> Create(CreateStaffAccountRequest request,
        CancellationToken cancellationToken)
    {
        var account = await staffAccounts.CreateAsync(request.Username, request.Password, cancellationToken);
        if (account is null)
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Username is already in use.");

        return StatusCode(StatusCodes.Status201Created, new StaffAccountResponse(account.Id, account.Username));
    }

    [HttpPost("{id:int}/lock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Lock(int id, CancellationToken cancellationToken)
    {
        if (!await staffAccounts.LockAsync(id, cancellationToken))
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Staff account not found.");

        return NoContent();
    }
}
