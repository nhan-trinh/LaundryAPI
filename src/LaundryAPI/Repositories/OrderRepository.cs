using System.Data;
using LaundryAPI.Data;
using LaundryAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LaundryAPI.Repositories;

public class OrderRepository(LaundryDbContext db)
{
    // Keep the quoted services active and at their observed prices until the receipt is saved.
    public Task<IDbContextTransaction> BeginReceiptAsync(CancellationToken ct) =>
        db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);

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
}
