using System.Data;
using LaundryAPI.Data;
using LaundryAPI.Dtos;
using LaundryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LaundryAPI.Repositories;

public class OrderRepository(LaundryDbContext db)
{
    public Task<List<OrderSummaryResponse>> SearchAsync(OrderSearchRequest request, CancellationToken ct)
    {
        var offset = (long)(request.Page - 1) * request.PageSize;
        if (offset > int.MaxValue)
            return Task.FromResult(new List<OrderSummaryResponse>());

        var orders = db.Orders.AsNoTracking();
        if (request.Code is not null)
            orders = orders.Where(o => o.Code == request.Code);
        if (request.PhoneNumber is not null)
            orders = orders.Where(o => db.Customers.Any(c => c.Id == o.CustomerId && c.PhoneNumber == request.PhoneNumber));
        if (request.Status is not null)
            orders = orders.Where(o => o.Status == request.Status);

        var receipts = from order in orders
                       from receipt in order.History
                       where receipt.Status == LaundryOrder.Received
                       select new { Order = order, ReceivedAt = receipt.OccurredAt };
        if (request.ReceivedDate is { } date)
        {
            var localDate = date.ToDateTime(TimeOnly.MinValue);
            receipts = receipts.Where(r => EF.Functions.AtTimeZone(r.ReceivedAt, "SE Asia Standard Time").Date == localDate);
        }

        return receipts.OrderByDescending(r => r.ReceivedAt).ThenByDescending(r => r.Order.Id)
            .Skip((int)offset).Take(request.PageSize)
            .Select(r => new OrderSummaryResponse(r.Order.Code, r.Order.CustomerId, r.Order.Status, r.ReceivedAt))
            .ToListAsync(ct);
    }

    // Keep the quoted services active and at their observed prices until the receipt is saved.
    public async Task<T> InReceiptTransactionAsync<T>(Func<Task<T>> receive, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var result = await receive();
        await transaction.CommitAsync(ct);
        return result;
    }

    public Task<bool> CustomerExistsAsync(int id, CancellationToken ct) =>
        db.Customers.AnyAsync(c => c.Id == id, ct);

    public Task<Dictionary<int, ServiceOffering>> FindActiveServicesAsync(int[] ids, CancellationToken ct) =>
        db.ServiceOfferings.AsNoTracking().Where(s => ids.Contains(s.Id) && s.IsActive)
            .ToDictionaryAsync(s => s.Id, ct);

    public async Task AddAsync(LaundryOrder order, CancellationToken ct)
    {
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
    }

    public Task<LaundryOrder?> FindAsync(string code, CancellationToken ct) =>
        db.Orders.AsNoTracking().Include(o => o.Lines).Include(o => o.History).ThenInclude(e => e.Account)
            .AsSingleQuery().SingleOrDefaultAsync(o => o.Code == code, ct);

    public async Task<OrderChangeResult> ChangeAsync(string code, byte[] version, string requiredStatus,
        Func<LaundryOrder, bool> tryApply, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var receipt = db.Orders.Where(o => o.Code == code);
        // Claim the parent row before touching lines or history. Even a line-only edit advances its rowversion.
        var updated = await receipt.Where(o => o.Status == requiredStatus && o.Version == version)
            .ExecuteUpdateAsync(set => set.SetProperty(o => o.Status, requiredStatus), ct);
        if (updated == 0)
        {
            return await receipt.AnyAsync(ct) ? OrderChangeResult.Conflict : OrderChangeResult.NotFound;
        }

        var order = await receipt.Include(o => o.Lines).SingleAsync(ct);
        if (!tryApply(order))
            return OrderChangeResult.NotFound;

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return OrderChangeResult.Saved;
    }
}
