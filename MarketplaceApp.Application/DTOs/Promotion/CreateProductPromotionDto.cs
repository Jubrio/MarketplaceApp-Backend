namespace MarketplaceApp.Application.DTOs.Promotion;

public class CreateProductPromotionDto
{
    public int ProductId { get; set; }
    public string Type { get; set; } = "Percentage";
    public decimal Value { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}