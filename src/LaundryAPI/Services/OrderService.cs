using System.ComponentModel.DataAnnotations;
using LaundryAPI.Dtos;
using LaundryAPI.Models;
using LaundryAPI.Repositories;

namespace LaundryAPI.Services;

public class OrderService(OrderRepository orders)
{
    public Task<OrderResponse> CreateAsync(CreateOrderRequest request, int accountId, CancellationToken ct) =>
        orders.InReceiptTransactionAsync(() => ReceiveAsync(request, accountId, ct), ct);

    private async Task<OrderResponse> ReceiveAsync(CreateOrderRequest request, int accountId, CancellationToken ct)
    {
        if (request.Lines.Any(line => line is null))
            throw new ValidationException("Each order line must specify a service and quantity.");

        if (!await orders.CustomerExistsAsync(request.CustomerId, ct))
            throw new ValidationException("Select an existing customer.");

        var ids = request.Lines.Select(l => l.ServiceId).Distinct().ToArray();
        var services = await orders.FindActiveServicesAsync(ids, ct);
        if (services.Count != ids.Length)
            throw new ValidationException("Select an active service for each order line.");

        var order = new LaundryOrder { CustomerId = request.CustomerId, PromisedAt = request.PromisedAt };
        foreach (var line in request.Lines)
        {
            var service = services[line.ServiceId];
            ValidateQuantity(service.Unit, line.Quantity);

            order.Lines.Add(new OrderLine
            {
                Position = order.Lines.Count, ServiceId = service.Id, ServiceName = service.Name,
                Unit = service.Unit, UnitPriceVnd = service.UnitPriceVnd, Quantity = line.Quantity, Note = line.Note
            });
        }
        order.History.Add(new OrderStatusEvent
        {
            Status = order.Status, AccountId = accountId, OccurredAt = DateTimeOffset.UtcNow
        });
        await orders.AddAsync(order, ct);
        try
        {
            return (await FindAsync(order.Code, ct))!;
        }
        catch (OverflowException)
        {
            throw new ValidationException("The order amount is too large.");
        }
    }

    public async Task<OrderResponse?> FindAsync(string code, CancellationToken ct)
    {
        var order = await orders.FindAsync(code, ct);
        if (order is null)
            return null;

        var lines = order.Lines.OrderBy(l => l.Position).Select(l => new OrderLineResponse(
            l.Id, l.ServiceId, l.ServiceName, l.Unit, l.UnitPriceVnd, l.Quantity, l.Note,
            CalculateAmountVnd(l.Quantity, l.UnitPriceVnd))).ToList();
        var history = order.History.OrderBy(e => e.Id).Select(e => new OrderEventResponse(
            e.Status, e.OccurredAt, e.AccountId, e.Account.Username)).ToList();
        return new OrderResponse(order.Code, order.CustomerId, order.PromisedAt, order.Status,
            order.Version, lines, lines.Sum(l => l.AmountVnd), history,
            order.CancellationReason, order.CancelledAt, order.CancelledBy);
    }

    public Task<OrderChangeResult> ChangeQuantityAsync(string code, int lineId,
        ChangeOrderQuantityRequest request, CancellationToken ct) =>
        orders.ChangeReceivedAsync(code, request.Version, order => TryChangeQuantity(order, lineId, request.Quantity), ct);

    private static bool TryChangeQuantity(LaundryOrder order, int lineId, decimal quantity)
    {
        var line = order.Lines.SingleOrDefault(l => l.Id == lineId);
        if (line is null)
            return false;
        ValidateQuantity(line.Unit, quantity);
        line.Quantity = quantity;
        try
        {
            _ = order.Lines.Sum(l => CalculateAmountVnd(l.Quantity, l.UnitPriceVnd));
        }
        catch (OverflowException)
        {
            throw new ValidationException("The order amount is too large.");
        }
        return true;
    }

    public Task<OrderChangeResult> CancelAsync(string code, CancelOrderRequest request, int accountId,
        CancellationToken ct) =>
        orders.ChangeReceivedAsync(code, request.Version, order =>
        {
            order.Cancel(request.CancellationReason.Trim(), accountId, DateTimeOffset.UtcNow);
            return true;
        }, ct);

    private static void ValidateQuantity(ServiceUnit unit, decimal quantity)
    {
        var decimals = unit == ServiceUnit.Kg ? 2 : 0;
        if (decimal.Round(quantity, decimals) != quantity)
            throw new ValidationException(unit == ServiceUnit.Kg
                ? "Kilograms allow at most two decimal places."
                : "Pieces must be whole numbers.");
    }

    private static decimal CalculateAmountVnd(decimal quantity, long unitPriceVnd)
    {
        // Round the fractional part separately so large whole amounts cannot erase a half đồng.
        var wholeUnits = decimal.Truncate(quantity);
        var fractionalVnd = (quantity - wholeUnits) * unitPriceVnd;
        return wholeUnits * unitPriceVnd + decimal.Round(fractionalVnd, 0, MidpointRounding.AwayFromZero);
    }
}
