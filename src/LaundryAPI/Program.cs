using LaundryAPI.Data;
using LaundryAPI.Models;
using LaundryAPI.Repositories;
using LaundryAPI.Services;
using LaundryAPI.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
services.AddControllers();
services.AddProblemDetails();
services.AddDbContext<LaundryDbContext>((provider, options) =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var hasher = provider.GetRequiredService<IPasswordHasher<Account>>();
    Task SeedManagerAsync(DbContext db, CancellationToken ct) =>
        ManagerSeed.RunAsync(new AccountRepository((LaundryDbContext)db), config, hasher, ct);

    options.UseSqlServer(config.GetConnectionString("Laundry"))
        .UseSeeding((db, _) => SeedManagerAsync(db, CancellationToken.None).GetAwaiter().GetResult())
        .UseAsyncSeeding((db, _, ct) => SeedManagerAsync(db, ct));
});
services.AddScoped<IAccountRepository, AccountRepository>();
services.AddScoped<IPasswordHasher<Account>, PasswordHasher<Account>>();
services.AddScoped<ILoginService, LoginService>();
services.AddScoped<IStaffAccountService, StaffAccountService>();
services.AddScoped<ICustomerRepository, CustomerRepository>();
services.AddScoped<ICustomerService, CustomerService>();
services.AddScoped<AccountTokenEvents>();
services.AddSingleton<JwtTokens>();
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<JwtTokens>((options, tokens) =>
    {
        options.MapInboundClaims = false;
        options.EventsType = typeof(AccountTokenEvents);
        options.TokenValidationParameters = tokens.ValidationParameters;
    }).ValidateOnStart();
services.AddAuthorization();
services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "LaundryAPI", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT"
    });
    options.OperationFilter<BearerOperationFilter>();
});

var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LaundryDbContext>();
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("v1/swagger.json", "LaundryAPI v1"));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
