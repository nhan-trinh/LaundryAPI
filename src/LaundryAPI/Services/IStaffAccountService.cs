using LaundryAPI.Dtos;

namespace LaundryAPI.Services;

public interface IStaffAccountService
{
    Task<StaffAccountResponse?> CreateAsync(string username, string password, CancellationToken cancellationToken);
    Task<bool> LockAsync(int id, CancellationToken cancellationToken);
}
