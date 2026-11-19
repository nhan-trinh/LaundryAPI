using LaundryAPI.Data;
using LaundryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LaundryAPI.Repositories;

public class AccountRepository(LaundryDbContext db)
{
    public Task<Account?> FindAsync(string username, CancellationToken cancellationToken) =>
        db.Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.Username == username, cancellationToken);

    public Task<bool> HasManagerAsync(CancellationToken cancellationToken) =>
        db.Accounts.AnyAsync(a => a.Role == Account.ManagerRole, cancellationToken);

    public async Task AddAsync(Account account, CancellationToken cancellationToken)
    {
        db.Accounts.Add(account);
        await db.SaveChangesAsync(cancellationToken);
    }
}
