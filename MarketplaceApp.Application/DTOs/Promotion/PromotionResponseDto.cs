namespace MarketplaceApp.Application.DTOs.Promotion;

public class PromotionResponseDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MaxUses { get; set; }
    public int CurrentUses { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}