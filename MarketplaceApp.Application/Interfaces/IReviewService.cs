using MarketplaceApp.Application.DTOs.Review;

namespace MarketplaceApp.Application.Interfaces;

public interface IReviewService
{
    Task<ReviewResponseDto> CreateReviewAsync(int buyerId, CreateReviewDto dto);
    Task<List<ReviewResponseDto>> GetReviewsByProductIdAsync(int productId);
    Task<decimal> GetAverageRatingAsync(int productId);
    Task<bool> CanUserReviewAsync(int buyerId, int productId);
    Task<int?> GetEligibleOrderItemIdAsync(int buyerId, int productId);
}