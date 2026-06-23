using MarketplaceApp.Application.DTOs.Order;

namespace MarketplaceApp.Application.Interfaces;

public interface IOrderService
{
    Task<List<OrderResponseDto>> CreateFromCartAsync(int buyerId, CreateOrderDto dto);
    Task<List<OrderResponseDto>> GetBuyerOrdersAsync(int buyerId);
    Task<List<OrderResponseDto>> GetVendorOrdersAsync(int shopId);
    Task<bool> UpdateStatusAsync(int orderId, string status, string? cancellationReason = null);
}