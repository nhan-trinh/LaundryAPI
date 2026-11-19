using LaundryAPI.Models;
using LaundryAPI.Repositories;
using Microsoft.AspNetCore.Identity;

namespace LaundryAPI.Services;

public class StaffAccountService(AccountRepository accounts, IPasswordHasher<Account> hasher)
{
    public async Task<Account?> CreateAsync(string username, string password, CancellationToken cancellationToken)
    {
        var account = new Account { Username = username.Trim(), Role = Account.StaffRole };
        account.PasswordHash = hasher.HashPassword(account, password);
        return await accounts.TryAddAsync(account, cancellationToken) ? account : null;
    }

    public Task<bool> LockAsync(int id, CancellationToken cancellationToken) =>
        accounts.LockStaffAsync(id, cancellationToken);
}
