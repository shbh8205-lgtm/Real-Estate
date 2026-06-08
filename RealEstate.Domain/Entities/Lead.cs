namespace RealEstate.Domain.Entities;

public class Lead
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public int ClientId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Property? Property { get; set; }
    public Client? Client { get; set; }
}
