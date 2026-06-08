using MediatR;

namespace RealEstate.Application.Leads.Commands;

public record CreateLeadCommand(int PropertyId, int ClientId, string Message) : IRequest<int>;
