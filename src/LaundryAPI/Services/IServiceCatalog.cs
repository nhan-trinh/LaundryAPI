using LaundryAPI.Dtos;

namespace LaundryAPI.Services;

public interface IServiceCatalog
{
    Task<IReadOnlyList<ServiceOfferingResponse>> ListActiveAsync(CancellationToken cancellationToken);
    Task<ServiceOfferingResponse> CreateAsync(ServiceOfferingRequest request, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(int id, ServiceOfferingRequest request, CancellationToken cancellationToken);
    Task<bool> DiscontinueAsync(int id, CancellationToken cancellationToken);
}
