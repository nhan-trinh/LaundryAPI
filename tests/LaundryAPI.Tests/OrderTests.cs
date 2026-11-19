using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using static LaundryAPI.Tests.HttpTestHelpers;

namespace LaundryAPI.Tests;

public class OrderTests
{
    private const string Orders = "/api/v1/orders";
    private sealed record Line(int ServiceId, string ServiceName, string Unit, long UnitPriceVnd,
        decimal Quantity, string? Note, decimal AmountVnd);
    private sealed record ReceivedEvent(string Status, int AccountId, string Username, DateTimeOffset OccurredAt);
    private sealed record Receipt(string Code, int CustomerId, DateTimeOffset? PromisedAt, string Status,
        string Version, Line[] Lines, decimal TotalVnd, ReceivedEvent[] History);

    [Fact]
    public async Task Staff_receives_mixed_laundry_and_manager_reads_the_priced_receipt_by_code()
    {
        await using var database = new SqlTestDatabase();
        var password = RandomSecret();
        using var host = database.Start(RandomSecret(), password);
        using var manager = host.CreateClient();
        await SignIn(manager, "manager", password);
        using var staff = await SignInStaff(host, manager);
        var customerId = await CreateCustomer(staff);
        var kg = await CreateService(manager, "Giặt sấy", "kg", 10002);
        var piece = await CreateService(manager, "Giặt chăn", "piece", 30000);
        var promisedAt = DateTimeOffset.UtcNow.AddDays(2);
        var before = DateTimeOffset.UtcNow;
        var request = new { customerId, promisedAt, lines = new object[] {
            new { serviceId = kg, quantity = 0.25m, note = "Túi xanh" },
            new { serviceId = piece, quantity = 2 } } };
        using var created = await staff.PostAsJsonAsync(Orders, request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var receipt = (await created.Content.ReadFromJsonAsync<Receipt>())!;
        Assert.Equal($"{Orders}/{receipt.Code}", created.Headers.Location!.AbsolutePath);
        var saved = (await manager.GetFromJsonAsync<Receipt>($"{Orders}/{receipt.Code}"))!;
        Assert.Equal(receipt.Code, saved.Code);
        Assert.Equal(customerId, saved.CustomerId);
        Assert.Equal(promisedAt, saved.PromisedAt);
        Assert.Equal("Received", saved.Status);
        Assert.False(string.IsNullOrWhiteSpace(saved.Version));
        Assert.Equal(receipt.Version, saved.Version);
        Assert.Equal([new Line(kg, "Giặt sấy", "kg", 10002, 0.25m, "Túi xanh", 2501),
            new Line(piece, "Giặt chăn", "piece", 30000, 2, null, 60000)], saved.Lines);
        Assert.Equal(62501, saved.TotalVnd);
        var received = Assert.Single(saved.History);
        Assert.Equal("Received", received.Status);
        Assert.Equal("linh", received.Username);
        Assert.InRange(received.OccurredAt, before, DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Invalid_customer_lines_and_quantities_return_problem_details()
    {
        await using var database = new SqlTestDatabase();
        var password = RandomSecret();
        using var host = database.Start(RandomSecret(), password);
        using var manager = host.CreateClient();
        await SignIn(manager, "manager", password);
        var customerId = await CreateCustomer(manager);
        var kg = await CreateService(manager, "Giặt sấy", "kg", 10002);
        var piece = await CreateService(manager, "Giặt chăn", "piece", 30000);
        var lines = new[] { new { serviceId = kg, quantity = 1m } };
        object[] invalid = [
            new { lines }, new { customerId = int.MaxValue, lines }, new { customerId },
            new { customerId, lines = Array.Empty<object>() }, new { customerId, lines = new object?[] { null } },
            new { customerId, lines = new[] { new { serviceId = int.MaxValue, quantity = 1 } } },
            new { customerId, lines = new[] { new { serviceId = kg, quantity = 0m } } },
            new { customerId, lines = new[] { new { serviceId = kg, quantity = -1m } } },
            new { customerId, lines = new[] { new { serviceId = kg, quantity = 0.251m } } },
            new { customerId, lines = new[] { new { serviceId = piece, quantity = 1.5m } } },
            new { customerId, lines = new[] { new { serviceId = piece, quantity = decimal.MaxValue } } }
        ];
        foreach (var request in invalid)
        {
            using var response = await manager.PostAsJsonAsync(Orders, request);
            await AssertProblem(response, HttpStatusCode.BadRequest);
        }
        var fractionalPiece = $$"""
            {"customerId":{{customerId}},"lines":[{"serviceId":{{piece}},"quantity":1.00000000000000000000000000001}]}
            """;
        using var body = new StringContent(fractionalPiece, System.Text.Encoding.UTF8, "application/json");
        using var inexact = await manager.PostAsync(Orders, body);
        await AssertProblem(inexact, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Quoted_lines_and_creator_survive_catalog_changes_and_account_lock()
    {
        await using var database = new SqlTestDatabase();
        var password = RandomSecret();
        using var host = database.Start(RandomSecret(), password);
        using var manager = host.CreateClient();
        await SignIn(manager, "manager", password);
        using var staff = await SignInStaff(host, manager);
        var customerId = await CreateCustomer(staff);
        var serviceId = await CreateService(manager, "Giặt sấy", "kg", 10002);
        var line = new { serviceId, quantity = 0.25m };
        var request = new { customerId, lines = new[] { line, line } };
        using var first = await staff.PostAsJsonAsync(Orders, request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var original = (await first.Content.ReadFromJsonAsync<Receipt>())!;
        using var second = await manager.PostAsJsonAsync(Orders, request);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.NotEqual(original.Code, (await second.Content.ReadFromJsonAsync<Receipt>())!.Code);

        var path = $"/api/v1/services/{serviceId}";
        using var update = await manager.PutAsJsonAsync(path, new { name = "Giặt áo", unit = "piece", unitPriceVnd = 900 });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        using var stop = await manager.PostAsync($"{path}/discontinue", null);
        Assert.Equal(HttpStatusCode.NoContent, stop.StatusCode);
        using var denied = await staff.PostAsJsonAsync(Orders, request);
        await AssertProblem(denied, HttpStatusCode.BadRequest);
        var creator = Assert.Single(original.History);
        using var locked = await manager.PostAsync($"/api/v1/staff-accounts/{creator.AccountId}/lock", null);
        Assert.Equal(HttpStatusCode.NoContent, locked.StatusCode);

        var saved = (await manager.GetFromJsonAsync<Receipt>($"{Orders}/{original.Code}"))!;
        var expected = new Line(serviceId, "Giặt sấy", "kg", 10002, 0.25m, null, 2501);
        Assert.Equal([expected, expected], saved.Lines);
        Assert.Equal(5002, saved.TotalVnd);
        Assert.Null(saved.PromisedAt);
        Assert.Equal(original.Version, saved.Version);
        Assert.Equal("linh", Assert.Single(saved.History).Username);
        using var blocked = await staff.GetAsync($"{Orders}/{original.Code}");
        await AssertProblem(blocked, HttpStatusCode.Unauthorized);
        using var unknown = await manager.GetAsync($"{Orders}/PG-0");
        await AssertProblem(unknown, HttpStatusCode.NotFound);
    }

    private static Task<int> CreateCustomer(HttpClient client) => CreateId(client, "/api/v1/customers",
        new { name = "Hà", phoneNumber = "0912345678" });

    private static Task<int> CreateService(HttpClient manager, string name, string unit, long unitPriceVnd) =>
        CreateId(manager, "/api/v1/services", new { name, unit, unitPriceVnd });

    private static async Task<int> CreateId(HttpClient client, string path, object body)
    {
        using var response = await client.PostAsJsonAsync(path, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }
}
