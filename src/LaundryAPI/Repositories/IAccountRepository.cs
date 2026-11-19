using LaundryAPI.Models;

namespace LaundryAPI.Repositories;

public interface IAccountRepository
{
    Task<Account?> FindAsync(string username, CancellationToken cancellationToken);
    Task<bool> HasManagerAsync(CancellationToken cancellationToken);
    Task<bool> IsActiveAsync(int id, CancellationToken cancellationToken);
    Task<bool> LockStaffAsync(int id, CancellationToken cancellationToken);
    Task<bool> TryAddAsync(Account account, CancellationToken cancellationToken);
}
