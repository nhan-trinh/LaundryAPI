using LaundryAPI.Dtos;
using LaundryAPI.Models;
using LaundryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryAPI.Controllers;

[ApiController]
[Route("api/v1/services")]
[Authorize(Roles = Account.StaffRole + "," + Account.ManagerRole)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public class ServicesController(IServiceCatalog catalog) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ServiceOfferingResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await catalog.ListActiveAsync(cancellationToken));

    [HttpPost]
    [Authorize(Roles = Account.ManagerRole)]
    [ProducesResponseType<ServiceOfferingResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceOfferingResponse>> Create(ServiceOfferingRequest request,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await catalog.CreateAsync(request, cancellationToken));

    [HttpPut("{id:int}")]
    [Authorize(Roles = Account.ManagerRole)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, ServiceOfferingRequest request, CancellationToken cancellationToken)
    {
        if (!await catalog.UpdateAsync(id, request, cancellationToken))
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Service not found.");

        return NoContent();
    }

    [HttpPost("{id:int}/discontinue")]
    [Authorize(Roles = Account.ManagerRole)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Discontinue(int id, CancellationToken cancellationToken)
    {
        if (!await catalog.DiscontinueAsync(id, cancellationToken))
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Service not found.");

        return NoContent();
    }
}
