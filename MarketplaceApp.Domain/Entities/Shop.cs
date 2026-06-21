namespace MarketplaceApp.Domain.Entities;

public class Shop
{
    public int Id { get; set; }
    public int VendorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public bool IsApproved { get; set; } = false;
    public decimal CommissionRate { get; set; } = 0.10m;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User Vendor { get; set; } = null!;
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}