using MarketplaceApp.Application.DTOs.Product;
using MarketplaceApp.Application.Interfaces;
using MarketplaceApp.Domain.Entities;
using MarketplaceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceApp.Application.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductDto>> GetAllAsync()
    {
        var now = DateTime.UtcNow;

        var activePromotionsDict = await _context.ProductPromotions
            .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
            .ToDictionaryAsync(p => p.ProductId);

        var products = await _context.Products
            .Include(p => p.Shop)
                .ThenInclude(s => s.Vendor)
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .ToListAsync();
            
        return products.Select(p =>
        {
            var hasPromo = activePromotionsDict.TryGetValue(p.Id, out var promo);
            decimal? discountedPrice = null;
            string? promoType = null;
            decimal? promoValue = null;

            if (hasPromo)
            {
                promoType = promo.Type;
                promoValue = promo.Value;
                discountedPrice = promo.Type == "Percentage"
                    ? p.Price * (1 - promo.Value / 100)
                    : p.Price - promo.Value;
            }

            return new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock,
                ImageUrl = p.ImageUrl,
                IsActive = p.IsActive,
                ShopId = p.ShopId,
                ShopName = p.Shop?.Name ?? "",
                VendorName = p.Shop?.Vendor?.FullName ?? "Vendeur inconnu",
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? "",
                LowStockThreshold = p.LowStockThreshold,
                HasPromotion = hasPromo,
                DiscountedPrice = discountedPrice,
                PromotionType = promoType,
                PromotionValue = promoValue
            };
        }).ToList();
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var now = DateTime.UtcNow;
        var product = await _context.Products
            .Include(p => p.Shop)
                .ThenInclude(s => s.Vendor)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return null;

        var promo = await _context.ProductPromotions
            .FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive && p.StartDate <= now && p.EndDate >= now);

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive,
            ShopId = product.ShopId,
            ShopName = product.Shop?.Name ?? "",
            VendorName = product.Shop?.Vendor?.FullName ?? "Vendeur inconnu",
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? "",
            LowStockThreshold = product.LowStockThreshold,
            HasPromotion = promo != null,
            DiscountedPrice = promo != null
                ? promo.Type == "Percentage" ? product.Price * (1 - promo.Value / 100) : product.Price - promo.Value
                : null,
            PromotionType = promo?.Type,
            PromotionValue = promo?.Value
        };
    }

    public async Task<ProductDto> CreateAsync(int shopId, CreateProductDto dto)
    {
        var product = new Product
        {
            ShopId = shopId,
            CategoryId = dto.CategoryId,
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            Stock = dto.Stock,
            ImageUrl = dto.ImageUrl,
            LowStockThreshold = dto.LowStockThreshold ?? 5
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(product.Id) ?? throw new Exception("Erreur création produit.");
    }

    public async Task<bool> UpdateAsync(int id, CreateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return false;

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.Stock = dto.Stock;
        product.ImageUrl = dto.ImageUrl;
        product.CategoryId = dto.CategoryId;
        if (dto.LowStockThreshold.HasValue)
            product.LowStockThreshold = dto.LowStockThreshold.Value;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return false;

        product.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }
}