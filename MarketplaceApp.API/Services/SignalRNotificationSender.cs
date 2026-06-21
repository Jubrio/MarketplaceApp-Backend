using MarketplaceApp.Application.DTOs.Notification;
using MarketplaceApp.Application.Interfaces;
using MarketplaceApp.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace MarketplaceApp.API.Services;

public class SignalRNotificationSender : INotificationSender
{
    private readonly IHubContext<NotificationHub> _hub;

    public SignalRNotificationSender(IHubContext<NotificationHub> hub)
    {
        _hub = hub;
    }

    public async Task SendNotificationAsync(int userId, NotificationDto notification)
    {
        await _hub.Clients.Group($"user_{userId}").SendAsync("NewNotification", notification);
    }
}