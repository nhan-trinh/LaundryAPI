using LaundryAPI.Models;

namespace LaundryAPI.Repositories;

public interface IServiceCatalogRepository
{
    Task<List<ServiceOffering>> ListActiveAsync(CancellationToken cancellationToken);
    Task AddAsync(ServiceOffering offering, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(int id, ServiceOffering offering, CancellationToken cancellationToken);
    Task<bool> DiscontinueAsync(int id, CancellationToken cancellationToken);
}
