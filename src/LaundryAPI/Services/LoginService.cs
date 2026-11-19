using LaundryAPI.Models;
using LaundryAPI.Repositories;
using Microsoft.AspNetCore.Identity;

namespace LaundryAPI.Services;

public class LoginService(AccountRepository accounts, IPasswordHasher<Account> hasher, JwtTokens tokens)
{
    public async Task<string?> LoginAsync(string username, string password, CancellationToken cancellationToken)
    {
        var account = await accounts.FindAsync(username.Trim(), cancellationToken);
        if (account is null || account.IsLocked ||
            hasher.VerifyHashedPassword(account, account.PasswordHash, password) == PasswordVerificationResult.Failed)
            return null;

        return tokens.Issue(account);
    }
}
