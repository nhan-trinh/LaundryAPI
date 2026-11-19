using System.Net;
using System.Net.Http.Json;
using Xunit;
using static LaundryAPI.Tests.HttpTestHelpers;

namespace LaundryAPI.Tests;

public class ServiceCatalogTests
{
    private const string Catalog = "/api/v1/services";
    private sealed record Offering(int Id, string Name, string Unit, long UnitPriceVnd);

    [Fact]
    public async Task Manager_creates_and_updates_a_service_and_staff_sees_its_current_price()
    {
        await using var database = new SqlTestDatabase();
        var password = RandomSecret();
        using var host = database.Start(RandomSecret(), password);
        using var manager = host.CreateClient();
        await SignIn(manager, "manager", password);

        var created = await Create(manager, "Giặt sấy", "kg", 10002);
        var catalog = await manager.GetFromJsonAsync<Offering[]>(Catalog);
        Assert.Equal(created, Assert.Single(catalog!));

        using var staff = await SignInStaff(host, manager);
        var updated = new Offering(created.Id, "Giặt chăn", "piece", 30000);
        using var change = await manager.PutAsJsonAsync($"{Catalog}/{created.Id}",
            new { updated.Name, updated.Unit, updated.UnitPriceVnd });
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);
        var staffCatalog = await staff.GetFromJsonAsync<Offering[]>(Catalog);
        Assert.Equal(updated, Assert.Single(staffCatalog!));
    }

    [Fact]
    public async Task Only_manager_can_change_prices_or_discontinue_a_service_for_new_lines()
    {
        await using var database = new SqlTestDatabase();
        var password = RandomSecret();
        using var host = database.Start(RandomSecret(), password);
        using var manager = host.CreateClient();
        await SignIn(manager, "manager", password);
        using var staff = await SignInStaff(host, manager);

        var kept = await Create(manager, "Giặt sấy", "kg", 10002);
        var discontinued = await Create(manager, "Giặt chăn", "piece", 30000);
        var path = $"{Catalog}/{discontinued.Id}";
        var request = new { name = "Ủi áo", unit = "piece", unitPriceVnd = 5000 };
        using var create = await staff.PostAsJsonAsync(Catalog, request);
        await AssertProblem(create, HttpStatusCode.Forbidden);
        using var update = await staff.PutAsJsonAsync(path, request);
        await AssertProblem(update, HttpStatusCode.Forbidden);
        using var forbidden = await staff.PostAsync($"{path}/discontinue", null);
        await AssertProblem(forbidden, HttpStatusCode.Forbidden);
        var before = await staff.GetFromJsonAsync<Offering[]>(Catalog);
        Assert.Equal(2, before!.Length);
        Assert.Contains(discontinued, before);

        using var stop = await manager.PostAsync($"{path}/discontinue", null);
        Assert.Equal(HttpStatusCode.NoContent, stop.StatusCode);
        using var staleUpdate = await manager.PutAsJsonAsync(path, request);
        await AssertProblem(staleUpdate, HttpStatusCode.NotFound);
        var available = await staff.GetFromJsonAsync<Offering[]>(Catalog);
        Assert.Equal(kept, Assert.Single(available!));
    }

    [Fact]
    public async Task Invalid_catalog_entries_return_problem_details_and_preserve_the_current_price()
    {
        await using var database = new SqlTestDatabase();
        var password = RandomSecret();
        using var host = database.Start(RandomSecret(), password);
        using var manager = host.CreateClient();
        using var anonymous = await manager.GetAsync(Catalog);
        await AssertProblem(anonymous, HttpStatusCode.Unauthorized);
        await SignIn(manager, "manager", password);
        var current = await Create(manager, "Giặt áo", "piece", 5000);
        object[] invalid = [
            new { name = " ", unit = "kg", unitPriceVnd = 10000 },
            new { name = "Giặt sấy", unitPriceVnd = 10000 },
            new { name = "Giặt sấy", unit = "litre", unitPriceVnd = 10000 },
            new { name = "Giặt sấy", unit = "kg, piece", unitPriceVnd = 10000 },
            new { name = "Giặt sấy", unit = 0, unitPriceVnd = 10000 },
            new { name = "Giặt sấy", unit = "kg" },
            new { name = "Giặt sấy", unit = "kg", unitPriceVnd = -1 },
            new { name = "Giặt sấy", unit = "kg", unitPriceVnd = 1.5m }
        ];
        foreach (var request in invalid)
        {
            using var create = await manager.PostAsJsonAsync(Catalog, request);
            await AssertProblem(create, HttpStatusCode.BadRequest);
            using var update = await manager.PutAsJsonAsync($"{Catalog}/{current.Id}", request);
            await AssertProblem(update, HttpStatusCode.BadRequest);
        }

        var catalog = await manager.GetFromJsonAsync<Offering[]>(Catalog);
        Assert.Equal(current, Assert.Single(catalog!));
    }

    private static async Task<Offering> Create(HttpClient manager, string name, string unit, long unitPriceVnd)
    {
        using var response = await manager.PostAsJsonAsync(Catalog, new { name, unit, unitPriceVnd });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var offering = await response.Content.ReadFromJsonAsync<Offering>();
        Assert.NotNull(offering);
        Assert.True(offering.Id > 0);
        Assert.Equal(new Offering(offering.Id, name, unit, unitPriceVnd), offering);
        return offering;
    }
}
