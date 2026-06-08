using MediatR;
using RealEstate.Application.Properties.Dtos;

namespace RealEstate.Application.Properties.Queries;

public record GetPropertiesQuery() : IRequest<IReadOnlyList<PropertyDto>>;
