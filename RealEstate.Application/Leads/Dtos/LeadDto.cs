namespace RealEstate.Application.Leads.Dtos;

public record LeadDto(
    int Id,
    int PropertyId,
    string PropertyTitle,
    int ClientId,
    string ClientName,
    string ClientEmail,
    string ClientPhone,
    string Message,
    DateTime CreatedAt
);
