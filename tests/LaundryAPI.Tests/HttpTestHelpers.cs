using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LaundryAPI.Tests;

internal static class HttpTestHelpers
{
    internal static string RandomSecret() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    internal static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)status, problem.GetProperty("status").GetInt32());
    }

    internal static async Task SignIn(HttpClient client, string username, string password)
    {
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { username, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            body.GetProperty("accessToken").GetString());
    }

    internal static async Task<HttpClient> SignInStaff(WebApplicationFactory<Program> host, HttpClient manager)
    {
        var password = RandomSecret();
        using var issued = await manager.PostAsJsonAsync("/api/v1/staff-accounts", new { username = "linh", password });
        Assert.Equal(HttpStatusCode.Created, issued.StatusCode);
        var staff = host.CreateClient();
        await SignIn(staff, "linh", password);
        return staff;
    }
}
