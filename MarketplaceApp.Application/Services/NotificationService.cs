using MarketplaceApp.Application.DTOs.Notification;
using MarketplaceApp.Application.Interfaces;
using MarketplaceApp.Domain.Entities;
using MarketplaceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceApp.Application.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly INotificationSender _sender;

    public NotificationService(AppDbContext context, INotificationSender sender)
    {
        _context = context;
        _sender = sender;
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(int userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();
        foreach (var n in notifications)
            n.IsRead = true;
        await _context.SaveChangesAsync();
    }

    public async Task CreateNotificationAsync(int userId, string title, string message, string type, string? link = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        var dto = new NotificationDto
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };

        await _sender.SendNotificationAsync(userId, dto);
    }

    private static string TranslateStatus(string status)
    {
        return status switch
        {
            "Pending" => "En attente",
            "Confirmed" => "Confirmée",
            "Shipped" => "Expédiée",
            "Delivered" => "Livrée",
            "Cancelled" => "Annulée",
            _ => status
        };
    }

    public async Task NotifyOrderStatusChangeAsync(int orderId, string oldStatus, string newStatus)
    {
        var order = await _context.Orders
            .Include(o => o.Buyer)
            .Include(o => o.Shop).ThenInclude(s => s.Vendor)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return;

        var oldTranslated = TranslateStatus(oldStatus);
        var newTranslated = TranslateStatus(newStatus);
        
        await CreateNotificationAsync(order.BuyerId,
            "Statut de commande mis à jour",
            $"Votre commande #{orderId} est '{newTranslated}'.",
            "Order", "/mes-commandes");

        if (newStatus == "Delivered")
        {
            await CreateNotificationAsync(order.Shop.VendorId,
                "Commande livrée",
                $"La commande #{orderId} a été livrée à {order.Buyer.FullName}.",
                "Order", "/vendor");
        }
    }

    public async Task NotifyNewOrderAsync(int shopId, int orderId)
    {
        var shop = await _context.Shops
            .Include(s => s.Vendor)
            .FirstOrDefaultAsync(s => s.Id == shopId);
        if (shop == null) return;

        await CreateNotificationAsync(shop.VendorId,
            "Nouvelle commande !",
            $"Commande #{orderId} passée dans votre boutique.",
            "Order", "/vendor");
    }

    public async Task NotifyShopApprovalRequestAsync(int shopId)
    {
        var shop = await _context.Shops
            .Include(s => s.Vendor)
            .FirstOrDefaultAsync(s => s.Id == shopId);
        if (shop == null) return;

        var admins = await _context.Users.Where(u => u.Role == "Admin").ToListAsync();
        foreach (var admin in admins)
            await CreateNotificationAsync(admin.Id,
                "Nouvelle boutique à valider",
                $"Boutique '{shop.Name}' de {shop.Vendor.FullName} demande validation.",
                "Shop", "/admin");
    }
    public async Task NotifyLowStockAsync(int shopId, int productId, int currentStock)
{
    var shop = await _context.Shops
        .Include(s => s.Vendor)
        .FirstOrDefaultAsync(s => s.Id == shopId);
    if (shop == null) return;

    var product = await _context.Products.FindAsync(productId);
    if (product == null) return;

    await CreateNotificationAsync(
        shop.VendorId,
        "Stock faible",
        $"Le stock du produit '{product.Name}' est bas ({currentStock} restants).",
        "System",
        $"/vendor"
    );
}
}