using LaundryAPI.Data;
using LaundryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LaundryAPI.Repositories;

public class ServiceCatalogRepository(LaundryDbContext db) : IServiceCatalogRepository
{
    public Task<List<ServiceOffering>> ListActiveAsync(CancellationToken cancellationToken) =>
        db.ServiceOfferings.AsNoTracking().Where(s => s.IsActive)
            .OrderBy(s => s.Name).ThenBy(s => s.Id).ToListAsync(cancellationToken);

    public async Task AddAsync(ServiceOffering offering, CancellationToken cancellationToken)
    {
        db.ServiceOfferings.Add(offering);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(int id, ServiceOffering offering, CancellationToken cancellationToken) =>
        await db.ServiceOfferings.Where(s => s.Id == id && s.IsActive).ExecuteUpdateAsync(setters => setters
            .SetProperty(s => s.Name, offering.Name)
            .SetProperty(s => s.Unit, offering.Unit)
            .SetProperty(s => s.UnitPriceVnd, offering.UnitPriceVnd), cancellationToken) == 1;

    public async Task<bool> DiscontinueAsync(int id, CancellationToken cancellationToken) =>
        await db.ServiceOfferings.Where(s => s.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.IsActive, false), cancellationToken) == 1;
}
