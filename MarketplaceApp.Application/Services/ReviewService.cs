using MarketplaceApp.Application.DTOs.Review;
using MarketplaceApp.Application.Interfaces;
using MarketplaceApp.Domain.Entities;
using MarketplaceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceApp.Application.Services;

public class ReviewService : IReviewService
{
    private readonly AppDbContext _context;

    public ReviewService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ReviewResponseDto> CreateReviewAsync(int buyerId, CreateReviewDto dto)
    {
        var orderItem = await _context.OrderItems
            .Include(oi => oi.Order)
            .Include(oi => oi.Product)
            .FirstOrDefaultAsync(oi => oi.Id == dto.OrderItemId);

        if (orderItem == null)
            throw new Exception("Article non trouvé.");

        if (orderItem.Order.BuyerId != buyerId)
            throw new Exception("Vous n'êtes pas l'acheteur de cet article.");

        if (orderItem.Order.Status != "Delivered")
            throw new Exception("Vous ne pouvez noter que les articles livrés.");

        var existing = await _context.Reviews
            .FirstOrDefaultAsync(r => r.OrderItemId == dto.OrderItemId);
        if (existing != null)
            throw new Exception("Vous avez déjà noté cet article.");

        var review = new Review
        {
            OrderItemId = dto.OrderItemId,
            BuyerId = buyerId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        var buyer = await _context.Users.FindAsync(buyerId);

        return new ReviewResponseDto
        {
            Id = review.Id,
            Rating = review.Rating,
            Comment = review.Comment,
            BuyerName = buyer?.FullName ?? "Anonyme",
            CreatedAt = review.CreatedAt,
            BuyerProfileImageUrl = buyer?.ProfileImageUrl
        };
    }

    public async Task<List<ReviewResponseDto>> GetReviewsByProductIdAsync(int productId)
    {
        return await _context.Reviews
            .Include(r => r.Buyer)
            .Where(r => r.OrderItem.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewResponseDto
            {
                Id = r.Id,
                Rating = r.Rating,
                Comment = r.Comment,
                BuyerName = r.Buyer.FullName,
                CreatedAt = r.CreatedAt,
                BuyerProfileImageUrl = r.Buyer.ProfileImageUrl
            })
            .ToListAsync();
    }

    public async Task<decimal> GetAverageRatingAsync(int productId)
    {
        var reviews = await _context.Reviews
            .Where(r => r.OrderItem.ProductId == productId)
            .ToListAsync();

        if (!reviews.Any()) return 0;

        var average = reviews.Average(r => (decimal)r.Rating);
        return Math.Round(average, 1);
    }

    public async Task<bool> CanUserReviewAsync(int buyerId, int productId)
    {
        var orderItem = await _context.OrderItems
            .Include(oi => oi.Order)
            .FirstOrDefaultAsync(oi =>
                oi.ProductId == productId &&
                oi.Order.BuyerId == buyerId &&
                oi.Order.Status == "Delivered");

        if (orderItem == null) return false;

        var hasReviewed = await _context.Reviews
            .AnyAsync(r => r.OrderItemId == orderItem.Id);

        return !hasReviewed;
    }

    public async Task<int?> GetEligibleOrderItemIdAsync(int buyerId, int productId)
{
    var orderItem = await _context.OrderItems
        .Include(oi => oi.Order)
        .FirstOrDefaultAsync(oi =>
            oi.ProductId == productId &&
            oi.Order.BuyerId == buyerId &&
            oi.Order.Status == "Delivered" &&
            !_context.Reviews.Any(r => r.OrderItemId == oi.Id));

    return orderItem?.Id;
}
}