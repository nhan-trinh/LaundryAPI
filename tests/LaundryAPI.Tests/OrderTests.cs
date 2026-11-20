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
    private sealed record OrderHistoryEvent(string Status, int AccountId, string Username, DateTimeOffset OccurredAt);
    private sealed record Payment(string Method, decimal AmountVnd);
    private sealed record Receipt(string Code, int CustomerId, DateTimeOffset? PromisedAt, string Status,
        string Version, Line[] Lines, decimal TotalVnd, OrderHistoryEvent[] History,
        string? CancellationReason, DateTimeOffset? CancelledAt, int? CancelledBy, Payment? Payment);

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

    [Fact]
    public async Task Staff_edits_quantity_using_the_quoted_unit_and_price_and_must_reload_before_another_edit()
    {
        await using var database = new SqlTestDatabase();
        var password = RandomSecret();
        using var host = database.Start(RandomSecret(), password);
        using var manager = host.CreateClient();
        await SignIn(manager, "manager", password);
        using var staff = await SignInStaff(host, manager);
        var kg = await CreateService(manager, "Giặt sấy", "kg", 10002);
        var piece = await CreateService(manager, "Giặt chăn", "piece", 30000);
        var customerId = await CreateCustomer(staff);
        using var created = await staff.PostAsJsonAsync(Orders, new { customerId, lines = new[] {
            new { serviceId = kg, quantity = 1 }, new { serviceId = piece, quantity = 2 } } });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var receipt = await created.Content.ReadFromJsonAsync<JsonElement>();
        var version = receipt.GetProperty("version").GetString();
        var path = $"{Orders}/{receipt.GetProperty("code").GetString()}";
        var kgPath = $"{path}/lines/{receipt.GetProperty("lines")[0].GetProperty("id")}/quantity";
        var piecePath = $"{path}/lines/{receipt.GetProperty("lines")[1].GetProperty("id")}/quantity";

        using var priceChange = await manager.PutAsJsonAsync($"/api/v1/services/{kg}",
            new { name = "Giặt áo", unit = "piece", unitPriceVnd = 900 });
        Assert.Equal(HttpStatusCode.NoContent, priceChange.StatusCode);
        foreach (var (url, quantity) in new[] { (kgPath, 0m), (kgPath, 0.251m), (piecePath, 1.5m),
                     (kgPath, 99999999999999999999999999m) })
        {
            using var invalid = await staff.PutAsJsonAsync(url, new { version, quantity });
            await AssertProblem(invalid, HttpStatusCode.BadRequest);
        }
        using var edit = await staff.PutAsJsonAsync(kgPath, new { version, quantity = 0.25m });
        Assert.Equal(HttpStatusCode.NoContent, edit.StatusCode);
        using var stale = await manager.PutAsJsonAsync(piecePath, new { version, quantity = 3 });
        await AssertProblem(stale, HttpStatusCode.Conflict);
        using var staleCancel = await manager.PostAsJsonAsync($"{path}/cancel", new { version, cancellationReason = "Trùng phiếu" });
        await AssertProblem(staleCancel, HttpStatusCode.Conflict);
        var saved = (await staff.GetFromJsonAsync<Receipt>(path))!;
        Assert.Equal("Received", saved.Status);
        Assert.Single(saved.History);
        Assert.NotEqual(version, saved.Version);
        Assert.Equal(62501, saved.TotalVnd);
        Assert.Equal(new Line(kg, "Giặt sấy", "kg", 10002, 0.25m, null, 2501), saved.Lines[0]);
        Assert.Equal(2, saved.Lines[1].Quantity);
    }

    [Fact]
    public async Task Concurrent_cancellations_preserve_the_receipt_and_record_only_the_successful_actor_and_reason()
    {
        await using var database = new SqlTestDatabase();
        var password = RandomSecret();
        using var host = database.Start(RandomSecret(), password);
        using var manager = host.CreateClient();
        await SignIn(manager, "manager", password);
        using var staff = await SignInStaff(host, manager);
        var serviceId = await CreateService(manager, "Giặt chăn", "piece", 30000);
        var customerId = await CreateCustomer(staff);
        using var created = await manager.PostAsJsonAsync(Orders, new { customerId, lines = new[] {
            new { serviceId, quantity = 1, note = "Chăn xanh" }, new { serviceId, quantity = 2, note = "Chăn đỏ" } } });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var receipt = await created.Content.ReadFromJsonAsync<JsonElement>();
        var original = receipt.Deserialize<Receipt>(JsonSerializerOptions.Web)!;
        var lineId = receipt.GetProperty("lines")[0].GetProperty("id").GetInt32();
        var path = $"{Orders}/{original.Code}";
        var cancel = $"{path}/cancel";
        var version = original.Version;
        foreach (var body in new object[] { new { version }, new { version, cancellationReason = "  " },
                     new { cancellationReason = "Sai phiếu" } })
        {
            using var invalid = await staff.PostAsJsonAsync(cancel, body);
            await AssertProblem(invalid, HttpStatusCode.BadRequest);
        }
        var received = (await staff.GetFromJsonAsync<Receipt>(path))!;
        Assert.Equal("Received", received.Status);
        Assert.Equal(version, received.Version);
        Assert.Null(received.CancelledAt);
        Assert.Null(received.CancelledBy);
        Assert.Null(received.CancellationReason);

        var before = DateTimeOffset.UtcNow;
        var outcomes = await Task.WhenAll(
            staff.PostAsJsonAsync(cancel, new { version, cancellationReason = "Khách đổi ý", cancelledBy = -1 }),
            manager.PostAsJsonAsync(cancel, new { version, cancellationReason = "Trùng phiếu" }));
        using var staffResult = outcomes[0];
        using var managerResult = outcomes[1];
        Assert.Single(outcomes, r => r.StatusCode == HttpStatusCode.NoContent);
        await AssertProblem(Assert.Single(outcomes, r => r.StatusCode != HttpStatusCode.NoContent), HttpStatusCode.Conflict);
        var saved = (await manager.GetFromJsonAsync<Receipt>(path))!;
        Assert.Equal("Cancelled", saved.Status);
        Assert.NotEqual(version, saved.Version);
        Assert.Equal(original.Lines, saved.Lines);
        Assert.Equal(original.TotalVnd, saved.TotalVnd);
        Assert.Equal(2, saved.History.Length);
        Assert.Equal(Assert.Single(original.History), saved.History[0]);
        var cancelled = saved.History[1];
        var staffWon = staffResult.IsSuccessStatusCode;
        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Equal(staffWon ? "linh" : "manager", cancelled.Username);
        Assert.Equal(staffWon ? "Khách đổi ý" : "Trùng phiếu", saved.CancellationReason);
        Assert.Equal(cancelled.AccountId, saved.CancelledBy);
        Assert.Equal(cancelled.OccurredAt, saved.CancelledAt);
        Assert.InRange(cancelled.OccurredAt, before, DateTimeOffset.UtcNow);

        using var edit = await manager.PutAsJsonAsync($"{path}/lines/{lineId}/quantity", new { saved.Version, quantity = 3 });
        await AssertProblem(edit, HttpStatusCode.Conflict);
        using var repeat = await staff.PostAsJsonAsync(cancel, new { saved.Version, cancellationReason = "Lý do mới" });
        await AssertProblem(repeat, HttpStatusCode.Conflict);
        var final = (await manager.GetFromJsonAsync<Receipt>(path))!;
        Assert.Equal(saved.Version, final.Version);
        Assert.Equal(saved.History, final.History);
        Assert.Equal(saved.Lines, final.Lines);
        Assert.Equal(saved.CancellationReason, final.CancellationReason);
    }

    [Fact]
    public async Task Starting_processing_locks_the_received_weight_and_records_the_staff_member()
    {
        await using var database = new SqlTestDatabase();
        var password = RandomSecret();
        using var host = database.Start(RandomSecret(), password);
        using var manager = host.CreateClient();
        await SignIn(manager, "manager", password);
        using var staff = await SignInStaff(host, manager);
        var (received, lineId) = await ReceiveFiveKilograms(manager);
        var path = $"{Orders}/{received.Code}";
        var before = DateTimeOffset.UtcNow;

        using var start = await staff.PostAsJsonAsync($"{path}/start-processing", new { received.Version });
        Assert.Equal(HttpStatusCode.NoContent, start.StatusCode);
        var processing = (await manager.GetFromJsonAsync<Receipt>(path))!;
        Assert.Equal("Processing", processing.Status);
        Assert.NotEqual(received.Version, processing.Version);
        Assert.Equal(2, processing.History.Length);
        Assert.Equal(Assert.Single(received.History), processing.History[0]);
        var started = processing.History[1];
        Assert.Equal("Processing", started.Status);
        Assert.Equal("linh", started.Username);
        Assert.InRange(started.OccurredAt, before, DateTimeOffset.UtcNow);

        using var repeat = await staff.PostAsJsonAsync($"{path}/start-processing", new { processing.Version });
        await AssertProblem(repeat, HttpStatusCode.Conflict);
        foreach (var client in new[] { staff, manager })
        {
            using var edit = await client.PutAsJsonAsync($"{path}/lines/{lineId}/quantity",
                new { processing.Version, quantity = 3 });
            await AssertProblem(edit, HttpStatusCode.Conflict);
            using var cancel = await client.PostAsJsonAsync($"{path}/cancel",
                new { processing.Version, cancellationReason = "Sai phiếu" });
            await AssertProblem(cancel, HttpStatusCode.Conflict);
        }
        var saved = (await staff.GetFromJsonAsync<Receipt>(path))!;
        Assert.Equal(processing.Version, saved.Version);
        Assert.Equal(processing.History, saved.History);
        Assert.Equal(5m, Assert.Single(saved.Lines).Quantity);
        Assert.Equal(50010m, saved.TotalVnd);
    }

    [Fact]
    public async Task Marking_the_whole_order_ready_requires_processing_and_its_current_version()
    {
        await using var database = new SqlTestDatabase();
        var password = RandomSecret();
        using var host = database.Start(RandomSecret(), password);
        using var manager = host.CreateClient();
        await SignIn(manager, "manager", password);
        using var staff = await SignInStaff(host, manager);
        var (received, _) = await ReceiveFiveKilograms(manager);
        var path = $"{Orders}/{received.Code}";
        var readyPath = $"{path}/mark-ready";

        using var skipped = await manager.PostAsJsonAsync(readyPath, new { received.Version });
        await AssertProblem(skipped, HttpStatusCode.Conflict);
        using var start = await staff.PostAsJsonAsync($"{path}/start-processing", new { received.Version });
        Assert.Equal(HttpStatusCode.NoContent, start.StatusCode);
        var processing = (await staff.GetFromJsonAsync<Receipt>(path))!;
        using var stale = await manager.PostAsJsonAsync(readyPath, new { received.Version });
        await AssertProblem(stale, HttpStatusCode.Conflict);

        var before = DateTimeOffset.UtcNow;
        using var confirm = await manager.PostAsJsonAsync(readyPath, new { processing.Version });
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
        var ready = (await staff.GetFromJsonAsync<Receipt>(path))!;
        Assert.Equal("Ready", ready.Status);
        Assert.NotEqual(processing.Version, ready.Version);
        Assert.Equal(received.Lines, ready.Lines);
        Assert.Equal(50010m, ready.TotalVnd);
        Assert.Equal(3, ready.History.Length);
        Assert.Equal(processing.History, ready.History[..2]);
        var confirmed = ready.History[2];
        Assert.Equal("Ready", confirmed.Status);
        Assert.Equal("manager", confirmed.Username);
        Assert.Equal(received.History[0].AccountId, confirmed.AccountId);
        Assert.InRange(confirmed.OccurredAt, before, DateTimeOffset.UtcNow);

        using var repeat = await staff.PostAsJsonAsync(readyPath, new { ready.Version });
        await AssertProblem(repeat, HttpStatusCode.Conflict);
        var saved = (await staff.GetFromJsonAsync<Receipt>(path))!;
        Assert.Equal(ready.Version, saved.Version);
        Assert.Equal(ready.History, saved.History);
    }

    [Fact]
    public async Task Concurrent_cancellation_and_processing_record_only_the_winning_step()
    {
        await using var database = new SqlTestDatabase();
        var password = RandomSecret();
        using var host = database.Start(RandomSecret(), password);
        using var manager = host.CreateClient();
        await SignIn(manager, "manager", password);
        using var staff = await SignInStaff(host, manager);
        var (received, _) = await ReceiveFiveKilograms(manager);
        var path = $"{Orders}/{received.Code}";

        var outcomes = await Task.WhenAll(
            staff.PostAsJsonAsync($"{path}/start-processing", new { received.Version }),
            manager.PostAsJsonAsync($"{path}/cancel", new { received.Version, cancellationReason = "Trùng phiếu" }));
        using var start = outcomes[0];
        using var cancel = outcomes[1];
        Assert.Single(outcomes, r => r.StatusCode == HttpStatusCode.NoContent);
        await AssertProblem(Assert.Single(outcomes, r => r.StatusCode != HttpStatusCode.NoContent), HttpStatusCode.Conflict);

        var saved = (await staff.GetFromJsonAsync<Receipt>(path))!;
        var cancelled = cancel.IsSuccessStatusCode;
        Assert.Equal(cancelled ? "Cancelled" : "Processing", saved.Status);
        Assert.NotEqual(received.Version, saved.Version);
        Assert.Equal(2, saved.History.Length);
        Assert.Equal(Assert.Single(received.History), saved.History[0]);
        var winner = saved.History[1];
        Assert.Equal(saved.Status, winner.Status);
        Assert.Equal(cancelled ? "manager" : "linh", winner.Username);
        Assert.Equal(cancelled ? "Trùng phiếu" : null, saved.CancellationReason);
        Assert.Equal(cancelled ? winner.AccountId : (int?)null, saved.CancelledBy);
        Assert.Equal(cancelled ? winner.OccurredAt : (DateTimeOffset?)null, saved.CancelledAt);
        Assert.Equal(received.Lines, saved.Lines);
        Assert.Equal(received.TotalVnd, saved.TotalVnd);
    }

    [Theory]
    [InlineData("cash")]
    [InlineData("bankTransfer")]
    public async Task Handover_collects_the_full_quoted_total_and_records_the_staff_member(string paymentMethod)
    {
        await using var database = new SqlTestDatabase();
        var password = RandomSecret();
        using var host = database.Start(RandomSecret(), password);
        using var manager = host.CreateClient();
        await SignIn(manager, "manager", password);
        using var staff = await SignInStaff(host, manager);
        var customerId = await CreateCustomer(manager);
        var kg = await CreateService(manager, "Giặt sấy", "kg", 10002);
        var piece = await CreateService(manager, "Giặt chăn", "piece", 30000);
        using var created = await manager.PostAsJsonAsync(Orders, new
        {
            customerId,
            lines = new[] { new { serviceId = kg, quantity = 0.25m }, new { serviceId = piece, quantity = 2m } }
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var received = (await created.Content.ReadFromJsonAsync<Receipt>())!;
        var path = $"{Orders}/{received.Code}";
        var ready = await MarkOrderReady(manager, received);
        Assert.Null(ready.Payment);

        var before = DateTimeOffset.UtcNow;
        using var handover = await staff.PostAsJsonAsync($"{path}/handover", new { ready.Version, paymentMethod });
        Assert.Equal(HttpStatusCode.NoContent, handover.StatusCode);
        var returned = (await manager.GetFromJsonAsync<Receipt>(path))!;
        Assert.Equal("Returned", returned.Status);
        Assert.NotEqual(ready.Version, returned.Version);
        Assert.Equal(62501m, returned.TotalVnd);
        Assert.Equal(new Payment(paymentMethod, 62501m), returned.Payment);
        Assert.Equal(ready.History, returned.History[..^1]);
        var handedOver = returned.History[^1];
        Assert.Equal("Returned", handedOver.Status);
        Assert.Equal("linh", handedOver.Username);
        Assert.InRange(handedOver.OccurredAt, before, DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Handover_requires_a_ready_order_its_current_version_and_a_supported_payment_method()
    {
        await using var database = new SqlTestDatabase();
        var password = RandomSecret();
        using var host = database.Start(RandomSecret(), password);
        using var manager = host.CreateClient();
        await SignIn(manager, "manager", password);
        var (received, _) = await ReceiveFiveKilograms(manager);
        var path = $"{Orders}/{received.Code}";
        var handoverPath = $"{path}/handover";

        using var early = await manager.PostAsJsonAsync(handoverPath, new { received.Version, paymentMethod = "cash" });
        await AssertProblem(early, HttpStatusCode.Conflict);
        var ready = await MarkOrderReady(manager, received);
        using var stale = await manager.PostAsJsonAsync(handoverPath, new { received.Version, paymentMethod = "cash" });
        await AssertProblem(stale, HttpStatusCode.Conflict);
        using var missing = await manager.PostAsJsonAsync(handoverPath, new { ready.Version });
        await AssertProblem(missing, HttpStatusCode.BadRequest);
        using var invalid = await manager.PostAsJsonAsync(handoverPath, new { ready.Version, paymentMethod = "card" });
        await AssertProblem(invalid, HttpStatusCode.BadRequest);
        var saved = (await manager.GetFromJsonAsync<Receipt>(path))!;
        Assert.Equal(ready.Version, saved.Version);
        Assert.Equal("Ready", saved.Status);
        Assert.Null(saved.Payment);
        Assert.Equal(ready.History, saved.History);
    }

    [Fact]
    public async Task Concurrent_handovers_and_a_retry_collect_once_and_record_only_the_winning_actor()
    {
        await using var database = new SqlTestDatabase();
        var password = RandomSecret();
        using var host = database.Start(RandomSecret(), password);
        using var manager = host.CreateClient();
        await SignIn(manager, "manager", password);
        using var staff = await SignInStaff(host, manager);
        var (received, _) = await ReceiveFiveKilograms(manager);
        var ready = await MarkOrderReady(manager, received);
        var path = $"{Orders}/{received.Code}";
        var handoverPath = $"{path}/handover";

        var responses = await Task.WhenAll(
            staff.PostAsJsonAsync(handoverPath, new { ready.Version, paymentMethod = "cash" }),
            manager.PostAsJsonAsync(handoverPath, new { ready.Version, paymentMethod = "bankTransfer" }));
        using var cash = responses[0];
        using var transfer = responses[1];
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.NoContent);
        await AssertProblem(Assert.Single(responses, response => !response.IsSuccessStatusCode), HttpStatusCode.Conflict);
        var returned = (await staff.GetFromJsonAsync<Receipt>(path))!;
        Assert.Equal("Returned", returned.Status);
        Assert.Equal(new Payment(cash.IsSuccessStatusCode ? "cash" : "bankTransfer", 50010m), returned.Payment);
        Assert.Equal(ready.History, returned.History[..^1]);
        Assert.Equal("Returned", returned.History[^1].Status);
        Assert.Equal(cash.IsSuccessStatusCode ? "linh" : "manager", returned.History[^1].Username);

        using var retry = await manager.PostAsJsonAsync(handoverPath, new { returned.Version, paymentMethod = "cash" });
        await AssertProblem(retry, HttpStatusCode.Conflict);
        var saved = (await manager.GetFromJsonAsync<Receipt>(path))!;
        Assert.Equal(returned.Version, saved.Version);
        Assert.Equal(returned.Payment, saved.Payment);
        Assert.Equal(returned.History, saved.History);
    }

    [Fact]
    public async Task Failure_to_record_handover_history_leaves_the_order_ready_and_unpaid()
    {
        await using var database = new SqlTestDatabase();
        var password = RandomSecret();
        using var host = database.Start(RandomSecret(), password);
        using var manager = host.CreateClient();
        await SignIn(manager, "manager", password);
        var (received, _) = await ReceiveFiveKilograms(manager);
        var ready = await MarkOrderReady(manager, received);
        var path = $"{Orders}/{received.Code}";
        await database.RejectReturnedHistoryAsync();

        using var failed = await manager.PostAsJsonAsync($"{path}/handover", new { ready.Version, paymentMethod = "cash" });
        await AssertProblem(failed, HttpStatusCode.InternalServerError);
        var saved = (await manager.GetFromJsonAsync<Receipt>(path))!;
        Assert.Equal("Ready", saved.Status);
        Assert.Equal(ready.Version, saved.Version);
        Assert.Null(saved.Payment);
        Assert.Equal(ready.History, saved.History);
    }

    private sealed record OrderSummary(string Code, int CustomerId, string Status, DateTimeOffset ReceivedAt);

    [Fact]
    public async Task Staff_finds_a_customers_receipts_by_phone_or_code_and_opens_cancelled_details()
    {
        await using var database = new SqlTestDatabase();
        var password = RandomSecret();
        using var host = database.Start(RandomSecret(), password);
        using var manager = host.CreateClient();
        await SignIn(manager, "manager", password);
        using var staff = await SignInStaff(host, manager);
        var customerId = await CreateCustomer(staff);
        var otherCustomer = await CreateId(staff, "/api/v1/customers", new { name = "An", phoneNumber = "0987654321" });
        var serviceId = await CreateService(manager, "Giặt chăn", "piece", 30000);
        var first = await ReceiveOrder(staff, customerId, serviceId);
        await ReceiveOrder(staff, otherCustomer, serviceId);
        var last = await ReceiveOrder(staff, customerId, serviceId);
        using var cancel = await staff.PostAsJsonAsync($"{Orders}/{first.Code}/cancel",
            new { first.Version, cancellationReason = "Khách đổi ý" });
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

        var cancelled = new OrderSummary(first.Code, customerId, "Cancelled", first.History[0].OccurredAt);
        var received = new OrderSummary(last.Code, customerId, "Received", last.History[0].OccurredAt);
        foreach (var phone in new[] { "0912345678", "%2B84912345678" })
            Assert.Equal([received, cancelled], (await staff.GetFromJsonAsync<OrderSummary[]>($"{Orders}?phoneNumber={phone}"))!);

        var match = Assert.Single((await manager.GetFromJsonAsync<OrderSummary[]>($"{Orders}?code={first.Code}"))!);
        Assert.Equal(cancelled, match);
        var detail = (await staff.GetFromJsonAsync<Receipt>($"{Orders}/{match.Code}"))!;
        Assert.Equal(match.Code, detail.Code);
        Assert.Equal(match.CustomerId, detail.CustomerId);
        Assert.Equal(match.Status, detail.Status);
        Assert.Equal(first.Lines, detail.Lines);
    }

    [Fact]
    public async Task Counter_filters_by_receipt_day_and_current_status_and_pages_across_tied_receipt_times()
    {
        await using var database = new SqlTestDatabase();
        var password = RandomSecret();
        using var host = database.Start(RandomSecret(), password);
        using var manager = host.CreateClient();
        await SignIn(manager, "manager", password);
        var customer = await CreateCustomer(manager);
        var service = await CreateService(manager, "Giặt sấy", "kg", 10002);
        var midnight = new DateTimeOffset(2026, 1, 12, 17, 0, 0, TimeSpan.Zero); // Jan 13 in Vietnam

        async Task<Receipt> ReceiveAt(DateTimeOffset time)
        {
            var receipt = await ReceiveOrder(manager, customer, service);
            await database.SetReceivedAtAsync(receipt.Code, time);
            return receipt;
        }

        var first = await ReceiveAt(midnight);
        var second = await ReceiveAt(midnight);
        var before = await ReceiveAt(midnight.AddTicks(-1));
        var end = await ReceiveAt(midnight.AddDays(1).AddTicks(-1));
        var after = await ReceiveAt(midnight.AddDays(1));
        using var start = await manager.PostAsJsonAsync($"{Orders}/{first.Code}/start-processing", new { first.Version });
        Assert.Equal(HttpStatusCode.NoContent, start.StatusCode);

        async Task<string[]> Search(string query) =>
            (await manager.GetFromJsonAsync<OrderSummary[]>($"{Orders}?{query}"))!.Select(row => row.Code).ToArray();

        Assert.Equal([end.Code, second.Code, first.Code], await Search("receivedDate=2026-01-13"));
        Assert.Equal([after.Code, end.Code, second.Code, before.Code], await Search("status=Received"));
        Assert.Equal([end.Code, second.Code], await Search("receivedDate=2026-01-13&status=Received"));
        Assert.Equal([end.Code, second.Code], await Search("receivedDate=2026-01-13&pageSize=2"));
        Assert.Equal([first.Code], await Search("receivedDate=2026-01-13&pageSize=2&page=2"));
        Assert.Empty(await Search("receivedDate=2026-01-13&pageSize=2&page=3"));
        Assert.Empty(await Search("page=2147483647&pageSize=100"));
        foreach (var invalid in new[] { "page=0", "pageSize=101", "status=unknown", "receivedDate=tomorrow", "phoneNumber=123" })
        {
            using var response = await manager.GetAsync($"{Orders}?{invalid}");
            await AssertProblem(response, HttpStatusCode.BadRequest);
        }
    }

    private static async Task<Receipt> ReceiveOrder(HttpClient client, int customerId, int serviceId)
    {
        using var response = await client.PostAsJsonAsync(Orders,
            new { customerId, lines = new[] { new { serviceId, quantity = 1 } } });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<Receipt>())!;
    }

    private static async Task<Receipt> MarkOrderReady(HttpClient client, Receipt received)
    {
        var path = $"{Orders}/{received.Code}";
        using var start = await client.PostAsJsonAsync($"{path}/start-processing", new { received.Version });
        Assert.Equal(HttpStatusCode.NoContent, start.StatusCode);
        var processing = (await client.GetFromJsonAsync<Receipt>(path))!;
        using var ready = await client.PostAsJsonAsync($"{path}/mark-ready", new { processing.Version });
        Assert.Equal(HttpStatusCode.NoContent, ready.StatusCode);
        return (await client.GetFromJsonAsync<Receipt>(path))!;
    }

    private static async Task<(Receipt Receipt, int LineId)> ReceiveFiveKilograms(HttpClient manager)
    {
        var serviceId = await CreateService(manager, "Giặt sấy", "kg", 10002);
        var customerId = await CreateCustomer(manager);
        using var response = await manager.PostAsJsonAsync(Orders,
            new { customerId, lines = new[] { new { serviceId, quantity = 5 } } });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (body.Deserialize<Receipt>(JsonSerializerOptions.Web)!, body.GetProperty("lines")[0].GetProperty("id").GetInt32());
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
