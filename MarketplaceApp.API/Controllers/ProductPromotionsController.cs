using MarketplaceApp.Application.DTOs.Promotion;
using MarketplaceApp.Application.Interfaces;
using MarketplaceApp.Infrastructure.Data;  
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;    
using System.Security.Claims;

namespace MarketplaceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Vendor")]
public class ProductPromotionsController : ControllerBase
{
    private readonly IProductPromotionService _promotionService;
    private readonly AppDbContext _context;

    public ProductPromotionsController(IProductPromotionService promotionService, AppDbContext context)
    {
        _promotionService = promotionService;
        _context = context;
    }

    [HttpGet("vendor")]
    public async Task<IActionResult> GetByVendor()
    {
        var vendorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var shop = await _context.Shops.FirstOrDefaultAsync(s => s.VendorId == vendorId);
        if (shop == null) return NotFound("Boutique introuvable.");

        var promos = await _promotionService.GetByShopIdAsync(shop.Id);
        return Ok(promos);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductPromotionDto dto)
    {
        try
        {
            var vendorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.VendorId == vendorId);
            if (shop == null) return NotFound("Boutique introuvable.");

            var result = await _promotionService.CreateAsync(shop.Id, dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _promotionService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}