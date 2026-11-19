using System.ComponentModel.DataAnnotations;
using LaundryAPI.Dtos;
using LaundryAPI.Models;
using LaundryAPI.Repositories;

namespace LaundryAPI.Services;

public class OrderService(OrderRepository orders)
{
    public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, int accountId, CancellationToken ct)
    {
        if (request.Lines.Any(line => line is null))
            throw new ValidationException("Each order line must specify a service and quantity.");

        await using var transaction = await orders.BeginReceiptAsync(ct);
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
            var decimals = service.Unit == ServiceUnit.Kg ? 2 : 0;
            if (decimal.Round(line.Quantity, decimals) != line.Quantity)
                throw new ValidationException(service.Unit == ServiceUnit.Kg
                    ? "Kilograms allow at most two decimal places."
                    : "Pieces must be whole numbers.");

            order.Lines.Add(new OrderLine
            {
                Position = order.Lines.Count, ServiceId = service.Id, ServiceName = service.Name,
                Unit = service.Unit, UnitPriceVnd = service.UnitPriceVnd, Quantity = line.Quantity, Note = line.Note
            });
        }
        try
        {
            _ = order.Lines.Sum(l => l.AmountVnd);
        }
        catch (OverflowException)
        {
            throw new ValidationException("The order amount is too large.");
        }
        order.History.Add(new OrderStatusEvent
        {
            Status = order.Status, AccountId = accountId, OccurredAt = DateTimeOffset.UtcNow
        });
        await orders.AddAsync(order, ct);
        var receipt = (await FindAsync(order.Code, ct))!;
        await transaction.CommitAsync(ct);
        return receipt;
    }

    public async Task<OrderResponse?> FindAsync(string code, CancellationToken ct)
    {
        var order = await orders.FindAsync(code, ct);
        if (order is null)
            return null;

        var lines = order.Lines.OrderBy(l => l.Position).Select(l => new OrderLineResponse(
            l.Id, l.ServiceId, l.ServiceName, l.Unit, l.UnitPriceVnd, l.Quantity, l.Note, l.AmountVnd)).ToList();
        var history = order.History.OrderBy(e => e.Id).Select(e => new OrderEventResponse(
            e.Status, e.OccurredAt, e.AccountId, e.Account.Username)).ToList();
        return new OrderResponse(order.Code, order.CustomerId, order.PromisedAt, order.Status,
            order.Version, lines, lines.Sum(l => l.AmountVnd), history);
    }
}
