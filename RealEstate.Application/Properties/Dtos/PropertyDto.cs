namespace RealEstate.Application.Properties.Dtos;

public record PropertyDto(
    int Id,
    string Title,
    string Description,
    decimal Price,
    string Address,
    DateTime CreatedAt,
    string Tags
);
