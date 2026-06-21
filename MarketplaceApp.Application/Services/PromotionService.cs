using MarketplaceApp.Application.DTOs.Promotion;
using MarketplaceApp.Application.Interfaces;
using MarketplaceApp.Domain.Entities;
using MarketplaceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceApp.Application.Services;

public class PromotionService : IPromotionService
{
    private readonly AppDbContext _context;

    public PromotionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<PromotionResponseDto>> GetAllAsync()
    {
        return await _context.Promotions
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PromotionResponseDto
            {
                Id = p.Id,
                Code = p.Code,
                Type = p.Type,
                Value = p.Value,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                MaxUses = p.MaxUses,
                CurrentUses = p.CurrentUses,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<PromotionResponseDto?> GetByIdAsync(int id)
    {
        var p = await _context.Promotions.FindAsync(id);
        if (p == null) return null;
        return new PromotionResponseDto
        {
            Id = p.Id,
            Code = p.Code,
            Type = p.Type,
            Value = p.Value,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            MaxUses = p.MaxUses,
            CurrentUses = p.CurrentUses,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt
        };
    }

    public async Task<PromotionResponseDto> CreateAsync(CreatePromotionDto dto)
    {
        var existing = await _context.Promotions.AnyAsync(p => p.Code == dto.Code);
        if (existing) throw new Exception("Ce code promo existe déjà.");

        var promotion = new Promotion
        {
            Code = dto.Code.ToUpper(),
            Type = dto.Type,
            Value = dto.Value,
            StartDate = dto.StartDate.ToUniversalTime(),
            EndDate = dto.EndDate.ToUniversalTime(),
            MaxUses = dto.MaxUses,
            IsActive = true
        };

        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(promotion.Id) ?? throw new Exception("Erreur création.");
    }

    public async Task<bool> UpdateAsync(int id, CreatePromotionDto dto)
    {
        var p = await _context.Promotions.FindAsync(id);
        if (p == null) return false;

        p.Code = dto.Code.ToUpper();
        p.Type = dto.Type;
        p.Value = dto.Value;
        p.StartDate = dto.StartDate.ToUniversalTime();
        p.EndDate = dto.EndDate.ToUniversalTime();
        p.MaxUses = dto.MaxUses;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var p = await _context.Promotions.FindAsync(id);
        if (p == null) return false;
        _context.Promotions.Remove(p);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<PromotionResultDto> ValidateAndApplyAsync(string code, decimal cartTotal)
    {
        var promo = await _context.Promotions
            .FirstOrDefaultAsync(p => p.Code == code.ToUpper() && p.IsActive);

        if (promo == null)
            return new PromotionResultDto { IsValid = false, Message = "Code promo invalide." };

        var now = DateTime.UtcNow;
        if (now < promo.StartDate || now > promo.EndDate)
            return new PromotionResultDto { IsValid = false, Message = "Code promo expiré ou pas encore actif." };

        if (promo.CurrentUses >= promo.MaxUses)
            return new PromotionResultDto { IsValid = false, Message = "Code promo déjà utilisé trop de fois." };

        decimal discount = promo.Type == "Percentage"
            ? cartTotal * (promo.Value / 100)
            : Math.Min(promo.Value, cartTotal); 

        return new PromotionResultDto
        {
            IsValid = true,
            DiscountAmount = discount,
            PromotionId = promo.Id,
            Message = $"Réduction de {discount:F2} € appliquée."
        };
    }

    public async Task<bool> IncrementUsageAsync(int promotionId)
    {
        var promo = await _context.Promotions.FindAsync(promotionId);
        if (promo == null) return false;
        promo.CurrentUses++;
        await _context.SaveChangesAsync();
        return true;
    }
}