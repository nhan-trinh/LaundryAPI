using LaundryAPI.Models;
using LaundryAPI.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LaundryAPI.Data;

public static class ManagerSeed
{
    public static async Task RunAsync(DbContext context, IConfiguration config,
        IPasswordHasher<Account> hasher, CancellationToken cancellationToken)
    {
        var accounts = new AccountRepository((LaundryDbContext)context);
        if (await accounts.HasManagerAsync(cancellationToken))
            return;

        var username = config["ManagerSeed:Username"]?.Trim();
        if (string.IsNullOrWhiteSpace(username) || username.Length > Account.UsernameMaxLength)
            throw new InvalidOperationException("Configure a valid ManagerSeed:Username for the first Manager.");

        if (await accounts.FindAsync(username, cancellationToken) is not null)
            throw new InvalidOperationException("The configured Manager username is already in use.");

        var password = config["ManagerSeed:Password"];
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Configure ManagerSeed:Password for the first Manager.");

        var manager = new Account { Username = username, Role = Account.ManagerRole };
        manager.PasswordHash = hasher.HashPassword(manager, password);
        if (!await accounts.TryAddAsync(manager, cancellationToken))
            throw new InvalidOperationException("The configured Manager username is already in use.");
    }
}
