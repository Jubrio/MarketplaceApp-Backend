namespace MarketplaceApp.Application.DTOs.Shop;

public class ShopDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public bool IsApproved { get; set; }
    public decimal CommissionRate { get; set; }
    public string VendorName { get; set; } = string.Empty;
}