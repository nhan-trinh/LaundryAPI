using LaundryAPI.Data;
using LaundryAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LaundryAPI.Repositories;

public class AccountRepository(LaundryDbContext db) : IAccountRepository
{
    public Task<Account?> FindAsync(string username, CancellationToken cancellationToken) =>
        db.Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.Username == username, cancellationToken);

    public Task<bool> HasManagerAsync(CancellationToken cancellationToken) =>
        db.Accounts.AnyAsync(a => a.Role == Account.ManagerRole, cancellationToken);

    public Task<bool> IsActiveAsync(int id, CancellationToken cancellationToken) =>
        db.Accounts.AnyAsync(a => a.Id == id && !a.IsLocked, cancellationToken);

    public async Task<bool> LockStaffAsync(int id, CancellationToken cancellationToken)
    {
        var matched = await db.Accounts.Where(a => a.Id == id && a.Role == Account.StaffRole)
            .ExecuteUpdateAsync(update => update.SetProperty(a => a.IsLocked, true), cancellationToken);
        return matched != 0;
    }

    public async Task<bool> TryAddAsync(Account account, CancellationToken cancellationToken)
    {
        db.Accounts.Add(account);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            db.Entry(account).State = EntityState.Detached;
            return false;
        }
    }
}
