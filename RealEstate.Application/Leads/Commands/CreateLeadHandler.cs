using MediatR;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Interfaces;

namespace RealEstate.Application.Leads.Commands;

public class CreateLeadHandler : IRequestHandler<CreateLeadCommand, int>
{
    private readonly IAsyncRepository<Lead> _leadRepo;
    private readonly IAsyncRepository<Property> _propertyRepo;

    public CreateLeadHandler(IAsyncRepository<Lead> leadRepo, IAsyncRepository<Property> propertyRepo)
    {
        _leadRepo = leadRepo;
        _propertyRepo = propertyRepo;
    }

    public async Task<int> Handle(CreateLeadCommand request, CancellationToken cancellationToken)
    {
        var property = await _propertyRepo.GetByIdAsync(request.PropertyId)
            ?? throw new InvalidOperationException("הנכס לא נמצא");

        var lead = new Lead
        {
            PropertyId = request.PropertyId,
            ClientId = request.ClientId,
            Message = request.Message,
            CreatedAt = DateTime.UtcNow
        };

        await _leadRepo.AddAsync(lead);
        return lead.Id;
    }
}
