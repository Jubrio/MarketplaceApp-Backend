using MarketplaceApp.Application.DTOs.Promotion;
using MarketplaceApp.Application.Interfaces;
using MarketplaceApp.Domain.Entities;
using MarketplaceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceApp.Application.Services;

public class ProductPromotionService : IProductPromotionService
{
    private readonly AppDbContext _context;

    public ProductPromotionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductPromotionResponseDto>> GetByShopIdAsync(int shopId)
    {
        return await _context.ProductPromotions
            .Include(p => p.Product)
            .Where(p => p.Product.ShopId == shopId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProductPromotionResponseDto
            {
                Id = p.Id,
                ProductId = p.ProductId,
                ProductName = p.Product.Name,
                Type = p.Type,
                Value = p.Value,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<ProductPromotionResponseDto> CreateAsync(int shopId, CreateProductPromotionDto dto)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId && p.ShopId == shopId);
        if (product == null)
            throw new Exception("Produit introuvable ou n'appartient pas à votre boutique.");

        var existing = await _context.ProductPromotions
            .FirstOrDefaultAsync(p => p.ProductId == dto.ProductId && p.IsActive);
        if (existing != null)
            throw new Exception("Ce produit a déjà une promotion active.");

        var promotion = new ProductPromotion
        {
            ProductId = dto.ProductId,
            Type = dto.Type,
            Value = dto.Value,
            StartDate = dto.StartDate.ToUniversalTime(),
            EndDate = dto.EndDate.ToUniversalTime(),
            IsActive = true
        };

        _context.ProductPromotions.Add(promotion);
        await _context.SaveChangesAsync();

        return new ProductPromotionResponseDto
        {
            Id = promotion.Id,
            ProductId = promotion.ProductId,
            ProductName = product.Name,
            Type = promotion.Type,
            Value = promotion.Value,
            StartDate = promotion.StartDate,
            EndDate = promotion.EndDate,
            IsActive = promotion.IsActive,
            CreatedAt = promotion.CreatedAt
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var promo = await _context.ProductPromotions.FindAsync(id);
        if (promo == null) return false;
        _context.ProductPromotions.Remove(promo);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ProductPromotion?> GetActivePromotionForProductAsync(int productId)
    {
        var now = DateTime.UtcNow;
        return await _context.ProductPromotions
            .FirstOrDefaultAsync(p =>
                p.ProductId == productId &&
                p.IsActive &&
                p.StartDate <= now &&
                p.EndDate >= now);
    }
}