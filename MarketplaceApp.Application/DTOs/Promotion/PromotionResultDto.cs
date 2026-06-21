namespace MarketplaceApp.Application.DTOs.Promotion;

public class PromotionResultDto
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public decimal DiscountAmount { get; set; }
    public int? PromotionId { get; set; }
}