using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using LaundryAPI.Models;
using Microsoft.IdentityModel.Tokens;

namespace LaundryAPI.Services;

public class JwtTokens
{
    private const string Issuer = "LaundryAPI";
    private const string Audience = "LaundryAPI.v1";
    private readonly SymmetricSecurityKey key;

    public JwtTokens(IConfiguration config)
    {
        var encoded = config["Jwt:SigningKey"] ?? throw new InvalidOperationException("Configure Jwt:SigningKey.");
        var bytes = Convert.FromBase64String(encoded);
        if (bytes.Length < 32)
            throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 random bytes in Base64.");
        key = new SymmetricSecurityKey(bytes);
    }

    public TokenValidationParameters ValidationParameters => new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = key,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ValidIssuer = Issuer,
        ValidAudience = Audience,
        ValidateLifetime = true,
        NameClaimType = "unique_name",
        RoleClaimType = "role"
    };

    public string Issue(Account account)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Expires = DateTime.UtcNow.AddHours(1),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = account.Id.ToString(CultureInfo.InvariantCulture),
                ["unique_name"] = account.Username,
                ["role"] = account.Role
            },
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };
        return new JwtSecurityTokenHandler().CreateEncodedJwt(descriptor);
    }
}
