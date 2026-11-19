using Microsoft.Extensions.Configuration;
using Xunit;

namespace LaundryAPI.Tests;

public class SecretConfigurationTests
{
    [Fact]
    public void Shipped_configuration_does_not_supply_a_seed_password_or_signing_key()
    {
        var files = Directory.GetFiles(AppContext.BaseDirectory, "appsettings*.json");
        Assert.NotEmpty(files);
        foreach (var path in files)
        {
            using var config = new ConfigurationManager();
            config.AddJsonFile(path);
            Assert.Null(config["ManagerSeed:Password"]);
            Assert.Null(config["Jwt:SigningKey"]);
        }
    }
}
