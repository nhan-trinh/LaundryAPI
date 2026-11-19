using System.Net;
using System.Net.Http.Json;
using Xunit;
using static LaundryAPI.Tests.HttpTestHelpers;

namespace LaundryAPI.Tests;

public class CustomerTests
{
    private const string Customers = "/api/v1/customers";
    private sealed record Profile(int Id, string Name, string PhoneNumber);

    [Fact]
    public async Task Staff_and_manager_can_create_and_find_the_same_profile_in_both_phone_formats()
    {
        await using var database = new SqlTestDatabase();
        var managerPassword = RandomSecret();
        using var host = database.Start(RandomSecret(), managerPassword);
        using var manager = host.CreateClient();
        await SignIn(manager, "manager", managerPassword);
        using var staff = await SignInStaff(host, manager);

        await CreateAndFind(staff, manager, "0912345678", "+84912345678");
        await CreateAndFind(manager, staff, "+84987654321", "0987654321");
    }

    [Fact]
    public async Task Concurrent_creates_and_a_later_family_member_reuse_the_first_profile()
    {
        await using var database = new SqlTestDatabase();
        var password = RandomSecret();
        using var host = database.Start(RandomSecret(), password);
        using var client = host.CreateClient();
        await SignIn(client, "manager", password);

        var replies = await Task.WhenAll(
            client.PostAsJsonAsync(Customers, new { name = "An", phoneNumber = "0912345678" }),
            client.PostAsJsonAsync(Customers, new { name = "Bình", phoneNumber = "+84912345678" }));
        using var created = Assert.Single(replies, reply => reply.StatusCode == HttpStatusCode.Created);
        using var existing = Assert.Single(replies, reply => reply.StatusCode == HttpStatusCode.OK);
        var original = await created.Content.ReadFromJsonAsync<Profile>();
        Assert.NotNull(original);
        Assert.Equal(created == replies[0] ? "An" : "Bình", original.Name);
        Assert.Equal(original, await existing.Content.ReadFromJsonAsync<Profile>());

        using var repeated = await client.PostAsJsonAsync(Customers, new { name = "Chi", phoneNumber = "0912345678" });
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        Assert.Equal(original, await repeated.Content.ReadFromJsonAsync<Profile>());
        Assert.Equal(original, await client.GetFromJsonAsync<Profile>(created.Headers.Location));
    }

    [Fact]
    public async Task Create_and_lookup_require_login_and_report_input_errors_as_problem_details()
    {
        await using var database = new SqlTestDatabase();
        var password = RandomSecret();
        using var host = database.Start(RandomSecret(), password);
        using var client = host.CreateClient();
        var lookup = $"{Customers}?phoneNumber=0912345678";
        using var anonymousCreate = await client.PostAsJsonAsync(Customers, new { name = "An", phoneNumber = "0912345678" });
        await AssertProblem(anonymousCreate, HttpStatusCode.Unauthorized);
        using var anonymousFind = await client.GetAsync(lookup);
        await AssertProblem(anonymousFind, HttpStatusCode.Unauthorized);
        await SignIn(client, "manager", password);

        object[] invalidRequests = [
            new { phoneNumber = "0912345678" },
            new { name = "An" },
            new { name = "   ", phoneNumber = "0912345678" },
            new { name = "An", phoneNumber = "" },
            new { name = "An", phoneNumber = "09123abc78" }
        ];
        foreach (var request in invalidRequests)
        {
            using var response = await client.PostAsJsonAsync(Customers, request);
            await AssertProblem(response, HttpStatusCode.BadRequest);
        }

        using var missingPhone = await client.GetAsync(Customers);
        await AssertProblem(missingPhone, HttpStatusCode.BadRequest);
        using var unknownCustomer = await client.GetAsync(lookup);
        await AssertProblem(unknownCustomer, HttpStatusCode.NotFound);
    }

    private static async Task CreateAndFind(HttpClient creator, HttpClient reader, string phoneNumber, string lookup)
    {
        using var created = await creator.PostAsJsonAsync(Customers, new { name = "Nguyễn An", phoneNumber });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var profile = await created.Content.ReadFromJsonAsync<Profile>();
        Assert.NotNull(profile);
        Assert.True(profile.Id > 0);
        Assert.Equal("Nguyễn An", profile.Name);
        Assert.Equal(phoneNumber.StartsWith('+') ? phoneNumber : lookup, profile.PhoneNumber);
        var found = await reader.GetFromJsonAsync<Profile>($"{Customers}?phoneNumber={Uri.EscapeDataString(lookup)}");
        Assert.Equal(profile, found);
    }
}
