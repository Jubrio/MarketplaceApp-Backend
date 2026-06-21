namespace MarketplaceApp.Application.DTOs.Product;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public int ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal? DiscountedPrice { get; set; }
    public bool HasPromotion { get; set; }
    public string? PromotionType { get; set; }
    public decimal? PromotionValue { get; set; }
    public int? LowStockThreshold { get; set; }
}