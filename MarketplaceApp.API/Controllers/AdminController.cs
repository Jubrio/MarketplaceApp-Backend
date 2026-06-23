using MarketplaceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace MarketplaceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        return Ok(new
        {
            totalUsers = await _context.Users.CountAsync(),
            totalShops = await _context.Shops.CountAsync(),
            totalProducts = await _context.Products.CountAsync(),
            totalOrders = await _context.Orders.CountAsync(),
            pendingShops = await _context.Shops.CountAsync(s => !s.IsApproved),
        });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.Role,
                u.IsActive,
                u.ProfileImageUrl, 
                u.CreatedAt
            }).ToListAsync();
        return Ok(users);
    }

    [HttpPut("users/{id}/toggle")]
    public async Task<IActionResult> ToggleUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();
        return Ok(new { message = $"Utilisateur {(user.IsActive ? "activé" : "désactivé")}." });
    }

    [HttpGet("shops")]
    public async Task<IActionResult> GetShops()
    {
        var shops = await _context.Shops
            .Include(s => s.Vendor)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.IsApproved,
                s.CommissionRate,
                s.CreatedAt,
                s.LogoUrl, 
                VendorName = s.Vendor.FullName,
                VendorEmail = s.Vendor.Email
            }).ToListAsync();
        return Ok(shops);
    }

    [HttpPut("shops/{id}/approve")]
    public async Task<IActionResult> ApproveShop(int id)
    {
        var shop = await _context.Shops.FindAsync(id);
        if (shop == null) return NotFound();
        shop.IsApproved = true;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Boutique approuvée." });
    }

    [HttpDelete("shops/{id}")]
    public async Task<IActionResult> DeleteShop(int id)
    {
        var shop = await _context.Shops.FindAsync(id);
        if (shop == null) return NotFound();
        _context.Shops.Remove(shop);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Boutique supprimée." });
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.Buyer)
            .Include(o => o.Shop)
            .Select(o => new
            {
                o.Id,
                o.Status,
                o.TotalAmount,
                o.CommissionAmount,
                o.CreatedAt,
                BuyerName = o.Buyer.FullName,
                ShopName = o.Shop.Name
            }).ToListAsync();
        return Ok(orders);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var cats = await _context.Categories.ToListAsync();
        return Ok(cats);
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
    {
        var cat = new MarketplaceApp.Domain.Entities.Category
        {
            Name = dto.Name,
            Slug = dto.Name.ToLower().Replace(" ", "-"),
            ParentId = dto.ParentId
        };
        _context.Categories.Add(cat);
        await _context.SaveChangesAsync();
        return Ok(cat);
    }

    [HttpDelete("categories/{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var cat = await _context.Categories.FindAsync(id);
        if (cat == null) return NotFound();
        _context.Categories.Remove(cat);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Catégorie supprimée." });
    }

    [HttpPost("create-admin")]
    public async Task<IActionResult> CreateAdmin([FromBody] RegisterAdminDto dto)
    {
        var exists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
        if (exists) return BadRequest(new { message = "Email déjà utilisé." });

        var user = new MarketplaceApp.Domain.Entities.User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "Admin",
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Compte admin créé avec succès." });
    }
}

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
}

public class RegisterAdminDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}