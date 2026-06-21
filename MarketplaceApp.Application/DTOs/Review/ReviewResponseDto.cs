namespace MarketplaceApp.Application.DTOs.Review;

public class ReviewResponseDto
{
    public int Id { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? BuyerProfileImageUrl { get; set; }
}