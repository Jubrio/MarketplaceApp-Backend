using MarketplaceApp.Application.DTOs.Order;
using MarketplaceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MarketplaceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    [Authorize(Roles = "Buyer")]
    public async Task<IActionResult> Create(CreateOrderDto dto)
    {
        try
        {
            var buyerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var orders = await _orderService.CreateFromCartAsync(buyerId, dto);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("my-orders")]
    [Authorize(Roles = "Buyer")]
    public async Task<IActionResult> GetMyOrders()
    {
        var buyerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var orders = await _orderService.GetBuyerOrdersAsync(buyerId);
        return Ok(orders);
    }

    [HttpGet("vendor-orders/{shopId}")]
    [Authorize(Roles = "Vendor")]
    public async Task<IActionResult> GetVendorOrders(int shopId)
    {
        var orders = await _orderService.GetVendorOrdersAsync(shopId);
        return Ok(orders);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Vendor,Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        Console.WriteLine($"[DEBUG] DTO reçu: Status={dto.Status}, CancellationReason={dto.CancellationReason}");

        var validStatuses = new[] { "Pending", "Confirmed", "Shipped", "Delivered", "Cancelled" };
        if (!validStatuses.Contains(dto.Status))
            return BadRequest(new { message = "Statut invalide." });

        var result = await _orderService.UpdateStatusAsync(id, dto.Status, dto.CancellationReason);
        if (!result) return NotFound();
        return Ok(new { message = "Statut mis à jour." });
    }
}

public class UpdateOrderStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? CancellationReason { get; set; }
}