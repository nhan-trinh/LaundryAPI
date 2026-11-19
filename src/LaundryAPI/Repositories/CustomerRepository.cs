using LaundryAPI.Data;
using LaundryAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LaundryAPI.Repositories;

public class CustomerRepository(LaundryDbContext db) : ICustomerRepository
{
    public Task<Customer?> FindAsync(string phoneNumber, CancellationToken cancellationToken) =>
        db.Customers.AsNoTracking().SingleOrDefaultAsync(c => c.PhoneNumber == phoneNumber, cancellationToken);

    public async Task<(Customer Customer, bool Created)> GetOrAddAsync(Customer customer, CancellationToken cancellationToken)
    {
        db.Customers.Add(customer);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return (customer, true);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            db.Entry(customer).State = EntityState.Detached;
            var existing = await FindAsync(customer.PhoneNumber, cancellationToken);
            if (existing is null)
                throw;

            return (existing, false);
        }
    }
}
