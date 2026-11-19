namespace LaundryAPI.Services;

public interface ILoginService
{
    Task<string?> LoginAsync(string username, string password, CancellationToken cancellationToken);
}
