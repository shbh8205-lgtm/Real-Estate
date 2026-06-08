using MediatR;
using RealEstate.Application.Properties.Dtos;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Interfaces;

namespace RealEstate.Application.Properties.Queries;

public class GetPropertiesHandler : IRequestHandler<GetPropertiesQuery, IReadOnlyList<PropertyDto>>
{
    private readonly IAsyncRepository<Property> _repository;

    public GetPropertiesHandler(IAsyncRepository<Property> repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<PropertyDto>> Handle(GetPropertiesQuery request, CancellationToken cancellationToken)
    {
        var properties = await _repository.ListAllAsync();

        return properties
            .Select(p => new PropertyDto(p.Id, p.Title, p.Description, p.Price, p.Address, p.CreatedAt, p.Tags))
            .ToList();
    }
}
