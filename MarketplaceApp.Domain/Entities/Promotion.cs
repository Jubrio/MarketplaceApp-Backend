namespace MarketplaceApp.Domain.Entities;

public class Promotion
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = "Percentage"; 
    public decimal Value { get; set; } 
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MaxUses { get; set; } = 1;
    public int CurrentUses { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}