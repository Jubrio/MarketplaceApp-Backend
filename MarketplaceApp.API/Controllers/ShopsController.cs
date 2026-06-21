using MarketplaceApp.Application.DTOs.Shop;
using MarketplaceApp.Domain.Entities;
using MarketplaceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarketplaceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShopsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ShopsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var shops = await _context.Shops
            .Include(s => s.Vendor)
            .Where(s => s.IsApproved)
            .Select(s => new ShopDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                LogoUrl = s.LogoUrl,
                IsApproved = s.IsApproved,
                CommissionRate = s.CommissionRate,
                VendorName = s.Vendor.FullName
            }).ToListAsync();

        return Ok(shops);
    }

    [HttpPost]
    [Authorize(Roles = "Vendor")]
    public async Task<IActionResult> Create(CreateShopDto dto)
    {
        var vendorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var exists = await _context.Shops.AnyAsync(s => s.VendorId == vendorId);
        if (exists) return BadRequest(new { message = "Vous avez déjà une boutique." });

        var shop = new Shop
        {
            VendorId = vendorId,
            Name = dto.Name,
            Description = dto.Description,
            LogoUrl = dto.LogoUrl
        };

        _context.Shops.Add(shop);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Boutique créée, en attente de validation.", shopId = shop.Id });
    }

    [HttpPut("{id}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(int id)
    {
        var shop = await _context.Shops.FindAsync(id);
        if (shop == null) return NotFound();

        shop.IsApproved = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Boutique approuvée." });
    }

    [HttpGet("my-shop")]
    [Authorize(Roles = "Vendor")]
    public async Task<IActionResult> GetMyShop()
    {
        var vendorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var shop = await _context.Shops
            .Include(s => s.Vendor)
            .FirstOrDefaultAsync(s => s.VendorId == vendorId);

        if (shop == null) return NotFound();

        return Ok(new ShopDto
        {
            Id = shop.Id,
            Name = shop.Name,
            Description = shop.Description,
            LogoUrl = shop.LogoUrl,
            IsApproved = shop.IsApproved,
            CommissionRate = shop.CommissionRate,
            VendorName = shop.Vendor.FullName
        });
    }
}