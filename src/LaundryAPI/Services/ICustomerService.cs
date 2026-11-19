using LaundryAPI.Dtos;

namespace LaundryAPI.Services;

public interface ICustomerService
{
    Task<CustomerResponse?> FindAsync(string phoneNumber, CancellationToken cancellationToken);
    Task<(CustomerResponse Customer, bool Created)> GetOrCreateAsync(CreateCustomerRequest request,
        CancellationToken cancellationToken);
}
