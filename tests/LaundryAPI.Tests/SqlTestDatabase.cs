using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace LaundryAPI.Tests;

internal sealed class SqlTestDatabase : IAsyncDisposable
{
    private readonly string name = $"LaundryTest_{Guid.NewGuid():N}";
    private readonly SqlConnectionStringBuilder connection = new(
        Environment.GetEnvironmentVariable("LAUNDRY_TEST_SQL")
        ?? @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true");

    public WebApplicationFactory<Program> Start(string signingKey, string? seedPassword = null)
    {
        connection.InitialCatalog = name;
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Laundry"] = connection.ConnectionString,
            ["ManagerSeed:Username"] = "manager",
            ["Jwt:SigningKey"] = signingKey
        };
        if (seedPassword is not null)
            settings["ManagerSeed:Password"] = seedPassword;

        return new ApiFactory(settings);
    }

    public async Task RejectReturnedHistoryAsync()
    {
        await using var sql = new SqlConnection(connection.ConnectionString);
        await sql.OpenAsync();
        await using var command = sql.CreateCommand();
        command.CommandText = "ALTER TABLE OrderStatusEvent ADD CHECK (Status <> 'Returned')";
        await command.ExecuteNonQueryAsync();
    }

    public async Task SetReceivedAtAsync(string code, DateTimeOffset receivedAt)
    {
        await using var sql = new SqlConnection(connection.ConnectionString);
        await sql.OpenAsync();
        await using var command = sql.CreateCommand();
        command.CommandText = """
            UPDATE e SET OccurredAt = @receivedAt
            FROM OrderStatusEvent e JOIN Orders o ON o.Id = e.LaundryOrderId
            WHERE o.Code = @code AND e.Status = 'Received'
            """;
        command.Parameters.AddWithValue("@code", code);
        command.Parameters.AddWithValue("@receivedAt", receivedAt);
        await command.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        connection.InitialCatalog = "master";
        await using var sql = new SqlConnection(connection.ConnectionString);
        await sql.OpenAsync();
        await using var drop = sql.CreateCommand();
        drop.CommandText = $"IF DB_ID('{name}') IS NOT NULL BEGIN ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]; END";
        await drop.ExecuteNonQueryAsync();
    }

    private sealed class ApiFactory(Dictionary<string, string?> settings) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(settings));
    }
}
