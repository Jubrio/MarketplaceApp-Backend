using MarketplaceApp.Application.DTOs.Product;
using MarketplaceApp.Application.Interfaces;
using MarketplaceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarketplaceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly AppDbContext _context;

    public ProductsController(IProductService productService, AppDbContext context)
    {
        _productService = productService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _productService.GetAllAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = "Vendor")]
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        try
        {
            var vendorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            Console.WriteLine($"[DEBUG] vendorId extrait du token = {vendorId}");

            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.VendorId == vendorId);
            Console.WriteLine($"[DEBUG] shop trouvée = {shop != null} (Id={shop?.Id}, VendorId={shop?.VendorId})");

            if (shop == null) return BadRequest(new { message = "Boutique introuvable." });

            var product = await _productService.CreateAsync(shop.Id, dto);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Vendor")]
    public async Task<IActionResult> Update(int id, CreateProductDto dto)
    {
        var result = await _productService.UpdateAsync(id, dto);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Vendor")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _productService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}