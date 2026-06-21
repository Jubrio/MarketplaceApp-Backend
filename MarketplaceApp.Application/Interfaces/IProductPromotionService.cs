using MarketplaceApp.Domain.Entities;
using MarketplaceApp.Application.DTOs.Promotion;

namespace MarketplaceApp.Application.Interfaces;

public interface IProductPromotionService
{
    Task<List<ProductPromotionResponseDto>> GetByShopIdAsync(int shopId);
    Task<ProductPromotionResponseDto> CreateAsync(int shopId, CreateProductPromotionDto dto);
    Task<bool> DeleteAsync(int id);
    Task<ProductPromotion?> GetActivePromotionForProductAsync(int productId);
}