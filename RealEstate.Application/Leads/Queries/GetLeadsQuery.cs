using MediatR;
using RealEstate.Application.Leads.Dtos;

namespace RealEstate.Application.Leads.Queries;

public record GetLeadsQuery() : IRequest<IReadOnlyList<LeadDto>>;
