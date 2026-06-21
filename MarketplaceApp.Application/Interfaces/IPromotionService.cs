using MarketplaceApp.Application.DTOs.Promotion;

namespace MarketplaceApp.Application.Interfaces;

public interface IPromotionService
{
    Task<List<PromotionResponseDto>> GetAllAsync();
    Task<PromotionResponseDto?> GetByIdAsync(int id);
    Task<PromotionResponseDto> CreateAsync(CreatePromotionDto dto);
    Task<bool> UpdateAsync(int id, CreatePromotionDto dto);
    Task<bool> DeleteAsync(int id);
    Task<PromotionResultDto> ValidateAndApplyAsync(string code, decimal cartTotal);
    Task<bool> IncrementUsageAsync(int promotionId);
}