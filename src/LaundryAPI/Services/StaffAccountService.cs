using LaundryAPI.Dtos;
using LaundryAPI.Models;
using LaundryAPI.Repositories;
using Microsoft.AspNetCore.Identity;

namespace LaundryAPI.Services;

public class StaffAccountService(IAccountRepository accounts, IPasswordHasher<Account> hasher) : IStaffAccountService
{
    public async Task<StaffAccountResponse?> CreateAsync(string username, string password, CancellationToken cancellationToken)
    {
        var account = new Account { Username = username.Trim(), Role = Account.StaffRole };
        account.PasswordHash = hasher.HashPassword(account, password);
        if (!await accounts.TryAddAsync(account, cancellationToken))
            return null;

        return new StaffAccountResponse(account.Id, account.Username);
    }

    public Task<bool> LockAsync(int id, CancellationToken cancellationToken) =>
        accounts.LockStaffAsync(id, cancellationToken);
}
