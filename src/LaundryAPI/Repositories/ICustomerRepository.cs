using LaundryAPI.Models;

namespace LaundryAPI.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> FindAsync(string phoneNumber, CancellationToken cancellationToken);
    Task<(Customer Customer, bool Created)> GetOrAddAsync(Customer customer, CancellationToken cancellationToken);
}
