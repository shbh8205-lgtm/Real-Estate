using MediatR;
using RealEstate.Application.Leads.Dtos;
using RealEstate.Domain.Interfaces;

namespace RealEstate.Application.Leads.Queries;

public class GetLeadsHandler : IRequestHandler<GetLeadsQuery, IReadOnlyList<LeadDto>>
{
    private readonly ILeadRepository _leadRepo;

    public GetLeadsHandler(ILeadRepository leadRepo) => _leadRepo = leadRepo;

    public async Task<IReadOnlyList<LeadDto>> Handle(GetLeadsQuery request, CancellationToken cancellationToken)
    {
        var leads = await _leadRepo.ListWithDetailsAsync();

        return leads.Select(l => new LeadDto(
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
    }
}
