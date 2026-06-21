namespace MarketplaceApp.Application.DTOs.Review;

public class CreateReviewDto
{
    public int OrderItemId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}