using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using static LaundryAPI.Tests.HttpTestHelpers;

namespace LaundryAPI.Tests;

public class StaffAccountTests
{
    private const string Accounts = "/api/v1/staff-accounts";

    [Fact]
    public async Task Only_manager_can_issue_and_lock_staff_accounts_and_lock_revokes_access_immediately()
    {
        await using var database = new SqlTestDatabase();
        var managerPassword = RandomSecret();
        using var host = database.Start(RandomSecret(), managerPassword);
        using var manager = host.CreateClient();
        await SignIn(manager, "manager", managerPassword);

        var password = RandomSecret();
        using var created = await manager.PostAsJsonAsync(Accounts, new { username = "linh", password });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var account = await created.Content.ReadFromJsonAsync<JsonElement>();
        var lockUrl = $"{Accounts}/{account.GetProperty("id").GetInt32()}/lock";

        using var staff = host.CreateClient();
        await SignIn(staff, "linh", password);
        var me = await staff.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        Assert.Equal("linh", me.GetProperty("username").GetString());
        Assert.Equal("Staff", me.GetProperty("role").GetString());

        using var forbiddenCreate = await staff.PostAsJsonAsync(Accounts, new { username = "bao", password });
        await AssertProblem(forbiddenCreate, HttpStatusCode.Forbidden);
        using var forbiddenLock = await staff.PostAsync(lockUrl, null);
        await AssertProblem(forbiddenLock, HttpStatusCode.Forbidden);

        using var locked = await manager.PostAsync(lockUrl, null);
        Assert.Equal(HttpStatusCode.NoContent, locked.StatusCode);
        using var revoked = await staff.GetAsync("/api/v1/auth/me");
        await AssertProblem(revoked, HttpStatusCode.Unauthorized);
        using var newLogin = await staff.PostAsJsonAsync("/api/v1/auth/login", new { username = "linh", password });
        await AssertProblem(newLogin, HttpStatusCode.Unauthorized);

        // The original id still identifies a Staff account after locking.
        using var repeatedLock = await manager.PostAsync(lockUrl, null);
        Assert.Equal(HttpStatusCode.NoContent, repeatedLock.StatusCode);
        using var missing = await manager.PostAsync($"{Accounts}/{int.MaxValue}/lock", null);
        await AssertProblem(missing, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Concurrent_creation_allows_one_account_and_locking_keeps_its_username_reserved()
    {
        await using var database = new SqlTestDatabase();
        var managerPassword = RandomSecret();
        using var host = database.Start(RandomSecret(), managerPassword);
        using var manager = host.CreateClient();
        await SignIn(manager, "manager", managerPassword);

        var request = new { username = "linh", password = RandomSecret() };
        var replies = await Task.WhenAll(manager.PostAsJsonAsync(Accounts, request),
            manager.PostAsJsonAsync(Accounts, request));
        using var created = Assert.Single(replies, r => r.StatusCode == HttpStatusCode.Created);
        using var duplicate = Assert.Single(replies, r => r != created);
        await AssertProblem(duplicate, HttpStatusCode.Conflict);

        var account = await created.Content.ReadFromJsonAsync<JsonElement>();
        using var locked = await manager.PostAsync($"{Accounts}/{account.GetProperty("id").GetInt32()}/lock", null);
        Assert.Equal(HttpStatusCode.NoContent, locked.StatusCode);
        using var reused = await manager.PostAsJsonAsync(Accounts, request);
        await AssertProblem(reused, HttpStatusCode.Conflict);
    }
}
