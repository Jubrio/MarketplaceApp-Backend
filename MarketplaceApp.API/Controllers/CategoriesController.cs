using MarketplaceApp.Application.DTOs.Category;
using MarketplaceApp.Domain.Entities;
using MarketplaceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        var categories = await _context.Categories.AsNoTracking().ToListAsync();

        var topLevel = categories
            .Where(c => c.ParentId == null)
            .Select(c => MapToDto(c, categories))
            .ToList();

        return Ok(topLevel);
    }

    private static CategoryDto MapToDto(Category c, List<Category> all)
    {
        return new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            ParentId = c.ParentId,
            SubCategories = all
                .Where(x => x.ParentId == c.Id)
                .Select(x => MapToDto(x, all))
                .ToList()
        };
    }
}