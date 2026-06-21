using MarketplaceApp.Application.DTOs.Notification;

namespace MarketplaceApp.Application.Interfaces;

public interface INotificationSender
{
    Task SendNotificationAsync(int userId, NotificationDto notification);
}