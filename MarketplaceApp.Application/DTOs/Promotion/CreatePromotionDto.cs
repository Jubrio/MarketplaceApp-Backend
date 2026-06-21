namespace MarketplaceApp.Application.DTOs.Promotion;

public class CreatePromotionDto
{
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = "Percentage";
    public decimal Value { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MaxUses { get; set; } = 1;
}