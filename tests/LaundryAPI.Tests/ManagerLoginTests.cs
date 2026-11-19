using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LaundryAPI.Tests;

[CollectionDefinition("Manager seed environment", DisableParallelization = true)]
public class ManagerSeedEnvironmentCollection { }

[Collection("Manager seed environment")]
public class ManagerLoginTests
{
    [Fact]
    public async Task Manager_can_use_Swagger_and_keeps_the_original_password_after_restart()
    {
        var connection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("LAUNDRY_TEST_SQL")
            ?? @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true");
        var database = $"LaundryTest_{Guid.NewGuid():N}";
        connection.InitialCatalog = "master";
        await using var sql = new SqlConnection(connection.ConnectionString);
        await sql.OpenAsync();
        connection.InitialCatalog = database;
        var password = RandomSecret();
        var signingKey = RandomSecret();
        var replacement = RandomSecret();
        const string seedVariable = "ManagerSeed__Password";
        var previousSeed = Environment.GetEnvironmentVariable(seedVariable);
        try
        {
            Environment.SetEnvironmentVariable(seedVariable, password);
            using (var host = new ApiFactory(connection.ConnectionString, signingKey))
            using (var client = host.CreateClient())
            {
                var ui = await client.GetStringAsync("/swagger/index.html");
                Assert.Contains("Swagger UI", ui);
                var doc = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
                Assert.Equal("bearer", doc.GetProperty("components").GetProperty("securitySchemes")
                    .GetProperty("Bearer").GetProperty("scheme").GetString());
                var paths = doc.GetProperty("paths");
                Assert.True(paths.GetProperty("/api/v1/auth/login").TryGetProperty("post", out _));
                Assert.True(paths.GetProperty("/api/v1/auth/me").GetProperty("get")
                    .GetProperty("security")[0].TryGetProperty("Bearer", out _));

                using var anonymous = await client.GetAsync("/api/v1/auth/me");
                await AssertUnauthorized(anonymous);
                using var wrongPassword = await Login(client, replacement);
                await AssertUnauthorized(wrongPassword);

                using var login = await Login(client, password);
                Assert.Equal(HttpStatusCode.OK, login.StatusCode);
                var body = await login.Content.ReadFromJsonAsync<JsonElement>();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                    body.GetProperty("accessToken").GetString());
                var account = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
                Assert.Equal("manager", account.GetProperty("username").GetString());
                Assert.Equal("Manager", account.GetProperty("role").GetString());
            }

            Environment.SetEnvironmentVariable(seedVariable, replacement);
            using var restarted = new ApiFactory(connection.ConnectionString, signingKey);
            using var secondClient = restarted.CreateClient();
            using var originalLogin = await Login(secondClient, password);
            Assert.Equal(HttpStatusCode.OK, originalLogin.StatusCode);
            using var replacementLogin = await Login(secondClient, replacement);
            await AssertUnauthorized(replacementLogin);
        }
        finally
        {
            Environment.SetEnvironmentVariable(seedVariable, previousSeed);
            await using var drop = sql.CreateCommand();
            drop.CommandText = $"IF DB_ID('{database}') IS NOT NULL BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; END";
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static string RandomSecret() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static Task<HttpResponseMessage> Login(HttpClient client, string password) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { username = "manager", password });

    private static async Task AssertUnauthorized(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(401, problem.GetProperty("status").GetInt32());
    }

    private sealed class ApiFactory(string connection, string key) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Laundry"] = connection,
                    ["ManagerSeed:Username"] = "manager",
                    ["Jwt:SigningKey"] = key
                }));
    }
}
