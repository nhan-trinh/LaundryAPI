using LaundryAPI.Dtos;
using LaundryAPI.Models;
using LaundryAPI.Repositories;

namespace LaundryAPI.Services;

public class CustomerService(ICustomerRepository customers) : ICustomerService
{
    public async Task<CustomerResponse?> FindAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        var customer = await customers.FindAsync(Normalize(phoneNumber), cancellationToken);
        return customer is null ? null : ToResponse(customer);
    }

    public async Task<(CustomerResponse Customer, bool Created)> GetOrCreateAsync(CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var customer = new Customer { Name = request.Name.Trim(), PhoneNumber = Normalize(request.PhoneNumber) };
        var (profile, created) = await customers.GetOrAddAsync(customer, cancellationToken);
        return (ToResponse(profile), created);
    }

    private static string Normalize(string phoneNumber) =>
        phoneNumber.StartsWith('0') ? "+84" + phoneNumber[1..] : phoneNumber;

    private static CustomerResponse ToResponse(Customer customer) =>
        new(customer.Id, customer.Name, customer.PhoneNumber);
}
