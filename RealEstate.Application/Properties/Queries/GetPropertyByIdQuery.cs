using MediatR;
using RealEstate.Application.Properties.Dtos;

namespace RealEstate.Application.Properties.Queries;

public record GetPropertyByIdQuery(int Id) : IRequest<PropertyDto?>;
