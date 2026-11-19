using System.Data;
using LaundryAPI.Data;
using LaundryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LaundryAPI.Repositories;

public class OrderRepository(LaundryDbContext db)
{
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

    public async Task<OrderChangeResult> ChangeReceivedAsync(string code, byte[] version,
        Func<LaundryOrder, bool> tryApply, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var receipt = db.Orders.Where(o => o.Code == code);
        // Claim the parent row before touching lines or history. Even a line-only edit advances its rowversion.
        var updated = await receipt.Where(o => o.Status == LaundryOrder.Received && o.Version == version)
            .ExecuteUpdateAsync(set => set.SetProperty(o => o.Status, LaundryOrder.Received), ct);
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
