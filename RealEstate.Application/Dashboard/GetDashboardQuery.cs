using MediatR;

namespace RealEstate.Application.Dashboard;

public record GetDashboardQuery() : IRequest<DashboardDto>;
