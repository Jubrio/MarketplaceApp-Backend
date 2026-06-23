using MarketplaceApp.Application.DTOs.Category;
using MarketplaceApp.Domain.Entities;
using MarketplaceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarketplaceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isVendor = User.IsInRole("Vendor");

        var query = _context.Categories.AsQueryable();

        if (isVendor && userId != null)
        {
            var vendorId = int.Parse(userId);
            query = query.Where(c => c.VendorId == null || c.VendorId == vendorId);
        }

        var categories = await query
            .Include(c => c.SubCategories)
            .ToListAsync();

        var root = categories.Where(c => c.ParentId == null).ToList();
        return Ok(root.Select(c => MapToDto(c, categories)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Vendor")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var isAdmin = User.IsInRole("Admin");

        var category = new Category
        {
            Name = dto.Name,
            Slug = dto.Name.ToLower().Replace(" ", "-"),
            ParentId = dto.ParentId,
            VendorId = isAdmin ? null : userId 
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return Ok(category);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Vendor")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateCategoryDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var isAdmin = User.IsInRole("Admin");

        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();

        if (!isAdmin && category.VendorId != userId)
            return Forbid("Vous ne pouvez modifier que vos propres catégories.");

        category.Name = dto.Name;
        category.Slug = dto.Name.ToLower().Replace(" ", "-");
        category.ParentId = dto.ParentId;

        await _context.SaveChangesAsync();
        return Ok(category);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Vendor")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var isAdmin = User.IsInRole("Admin");

        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();

        if (!isAdmin && category.VendorId != userId)
            return Forbid("Vous ne pouvez supprimer que vos propres catégories.");

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private CategoryResponseDto MapToDto(Category c, List<Category> all)
    {
        return new CategoryResponseDto
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            ParentId = c.ParentId,
            VendorId = c.VendorId,
            SubCategories = all
                .Where(sub => sub.ParentId == c.Id)
                .Select(sub => MapToDto(sub, all))
                .ToList()
        };
    }
}