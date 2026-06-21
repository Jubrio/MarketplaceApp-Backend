namespace MarketplaceApp.Domain.Entities;

public class ProductPromotion
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Type { get; set; } = "Percentage"; 
    public decimal Value { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Product Product { get; set; } = null!;
}