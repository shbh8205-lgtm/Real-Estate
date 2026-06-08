using MediatR;
using RealEstate.Application.Leads.Dtos;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Interfaces;

namespace RealEstate.Application.Dashboard;

public class GetDashboardHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private readonly IAsyncRepository<Property> _propertyRepo;
    private readonly ILeadRepository _leadRepo;

    public GetDashboardHandler(IAsyncRepository<Property> propertyRepo, ILeadRepository leadRepo)
    {
        _propertyRepo = propertyRepo;
        _leadRepo = leadRepo;
    }

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var properties = await _propertyRepo.ListAllAsync();
        var recentLeads = await _leadRepo.ListWithDetailsAsync(take: 5);
        var allLeads = await _leadRepo.ListAllAsync();

        var avg = properties.Count > 0 ? properties.Average(p => p.Price) : 0m;
        var min = properties.Count > 0 ? properties.Min(p => p.Price) : 0m;
        var max = properties.Count > 0 ? properties.Max(p => p.Price) : 0m;

        var leadDtos = recentLeads.Select(l => new LeadDto(
            l.Id,
            l.PropertyId,
            l.Property?.Title ?? string.Empty,
            l.ClientId,
            l.Client?.FullName ?? string.Empty,
            l.Client?.Email ?? string.Empty,
            l.Client?.PhoneNumber ?? string.Empty,
            l.Message,
            l.CreatedAt
        )).ToList();

        return new DashboardDto(
            TotalProperties: properties.Count,
            AveragePrice: avg,
            MinPrice: min,
            MaxPrice: max,
            TotalLeads: allLeads.Count,
            RecentLeads: leadDtos
        );
    }
}
