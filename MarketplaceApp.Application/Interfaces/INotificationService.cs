using MarketplaceApp.Application.DTOs.Notification;

namespace MarketplaceApp.Application.Interfaces;

public interface INotificationService
{
    Task<List<NotificationDto>> GetUserNotificationsAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkAsReadAsync(int notificationId);
    Task MarkAllAsReadAsync(int userId);
    Task CreateNotificationAsync(int userId, string title, string message, string type, string? link = null);
    Task NotifyOrderStatusChangeAsync(int orderId, string oldStatus, string newStatus);
    Task NotifyNewOrderAsync(int shopId, int orderId);
    Task NotifyShopApprovalRequestAsync(int shopId);
    Task NotifyLowStockAsync(int shopId, int productId, int currentStock);
}