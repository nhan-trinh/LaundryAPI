using LaundryAPI.Dtos;
using LaundryAPI.Models;
using LaundryAPI.Repositories;

namespace LaundryAPI.Services;

public class ServiceCatalog(IServiceCatalogRepository catalog) : IServiceCatalog
{
    public async Task<IReadOnlyList<ServiceOfferingResponse>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var offerings = await catalog.ListActiveAsync(cancellationToken);
        return offerings.Select(ToResponse).ToList();
    }

    public async Task<ServiceOfferingResponse> CreateAsync(ServiceOfferingRequest request, CancellationToken cancellationToken)
    {
        var offering = ToOffering(request);
        await catalog.AddAsync(offering, cancellationToken);
        return ToResponse(offering);
    }

    public Task<bool> UpdateAsync(int id, ServiceOfferingRequest request, CancellationToken cancellationToken) =>
        catalog.UpdateAsync(id, ToOffering(request), cancellationToken);

    public Task<bool> DiscontinueAsync(int id, CancellationToken cancellationToken) =>
        catalog.DiscontinueAsync(id, cancellationToken);

    private static ServiceOffering ToOffering(ServiceOfferingRequest request) =>
        new() { Name = request.Name.Trim(), Unit = request.Unit, UnitPriceVnd = request.UnitPriceVnd };

    private static ServiceOfferingResponse ToResponse(ServiceOffering offering) =>
        new(offering.Id, offering.Name, offering.Unit, offering.UnitPriceVnd);
}
