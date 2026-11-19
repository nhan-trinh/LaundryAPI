using LaundryAPI.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace LaundryAPI.Services;

public class AccountTokenEvents(AccountRepository accounts) : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        if (!int.TryParse(context.Principal?.FindFirst("sub")?.Value, out var accountId) ||
            !await accounts.IsActiveAsync(accountId, context.HttpContext.RequestAborted))
            context.Fail("Account is unavailable.");
    }
}
