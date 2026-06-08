using MediatR;
using RealEstate.Application.Properties.Dtos;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Interfaces;

namespace RealEstate.Application.Properties.Queries;

public class GetPropertyByIdHandler : IRequestHandler<GetPropertyByIdQuery, PropertyDto?>
{
    private readonly IAsyncRepository<Property> _repository;

    public GetPropertyByIdHandler(IAsyncRepository<Property> repository)
    {
        _repository = repository;
    }

    public async Task<PropertyDto?> Handle(GetPropertyByIdQuery request, CancellationToken cancellationToken)
    {
        var p = await _repository.GetByIdAsync(request.Id);
        if (p is null) return null;

        return new PropertyDto(p.Id, p.Title, p.Description, p.Price, p.Address, p.CreatedAt, p.Tags);
    }
}
