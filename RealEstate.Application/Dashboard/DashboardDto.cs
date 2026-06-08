using RealEstate.Application.Leads.Dtos;

namespace RealEstate.Application.Dashboard;

public record DashboardDto(
    int TotalProperties,
    decimal AveragePrice,
    decimal MinPrice,
    decimal MaxPrice,
    int TotalLeads,
    IReadOnlyList<LeadDto> RecentLeads
);
