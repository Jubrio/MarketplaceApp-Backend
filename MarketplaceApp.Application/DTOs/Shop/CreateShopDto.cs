namespace MarketplaceApp.Application.DTOs.Shop;

public class CreateShopDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
}