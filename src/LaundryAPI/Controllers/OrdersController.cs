using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using LaundryAPI.Dtos;
using LaundryAPI.Models;
using LaundryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryAPI.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize(Roles = Account.StaffRole + "," + Account.ManagerRole)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public class OrdersController(OrderService orders) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        try
        {
            var created = await orders.CreateAsync(request, CurrentAccountId, ct);
            return CreatedAtAction(nameof(Find), new { code = created.Code }, created);
        }
        catch (ValidationException error)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: error.Message);
        }
    }

    [HttpPut("{code}/lines/{lineId:int}/quantity")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeQuantity(string code, int lineId, ChangeOrderQuantityRequest request,
        CancellationToken ct)
    {
        try
        {
            return ChangeResponse(await orders.ChangeQuantityAsync(code, lineId, request, ct));
        }
        catch (ValidationException error)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: error.Message);
        }
    }

    [HttpPost("{code}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(string code, CancelOrderRequest request, CancellationToken ct) =>
        ChangeResponse(await orders.CancelAsync(code, request, CurrentAccountId, ct));

    [HttpPost("{code}/start-processing")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartProcessing(string code, OrderProgressRequest request, CancellationToken ct) =>
        ChangeResponse(await orders.StartProcessingAsync(code, request, CurrentAccountId, ct));

    [HttpPost("{code}/mark-ready")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarkReady(string code, OrderProgressRequest request, CancellationToken ct) =>
        ChangeResponse(await orders.MarkReadyAsync(code, request, CurrentAccountId, ct));

    [HttpPost("{code}/handover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Handover(string code, HandoverOrderRequest request, CancellationToken ct) =>
        ChangeResponse(await orders.HandoverAsync(code, request, CurrentAccountId, ct));

    private int CurrentAccountId => int.Parse(User.FindFirstValue("sub")!, CultureInfo.InvariantCulture);

    private IActionResult ChangeResponse(OrderChangeResult result) => result switch
    {
        OrderChangeResult.Saved => NoContent(),
        OrderChangeResult.NotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Order or line not found."),
        OrderChangeResult.Conflict => Problem(statusCode: StatusCodes.Status409Conflict,
            title: "The order version or status does not allow this change. Reload the order."),
        _ => throw new ArgumentOutOfRangeException(nameof(result))
    };

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<OrderSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] OrderSearchRequest request, CancellationToken ct) =>
        Ok(await orders.SearchAsync(request, ct));

    [HttpGet("{code}")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> Find(string code, CancellationToken ct)
    {
        var order = await orders.FindAsync(code, ct);
        if (order is null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Order not found.");

        return Ok(order);
    }
}
