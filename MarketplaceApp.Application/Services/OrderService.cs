using MarketplaceApp.Application.DTOs.Order;
using MarketplaceApp.Application.Interfaces;
using MarketplaceApp.Domain.Entities;
using MarketplaceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceApp.Application.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IPromotionService _promotionService;
    private readonly IEmailService _emailService;

    public OrderService(AppDbContext context, INotificationService notificationService, IPromotionService promotionService, IEmailService emailService)
    {
        _context = context;
        _notificationService = notificationService;
        _promotionService = promotionService;
        _emailService = emailService;
    }

    public async Task<List<OrderResponseDto>> CreateFromCartAsync(int buyerId, CreateOrderDto dto)
    {
        var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        var productsDict = await _context.Products
            .Include(p => p.Shop)
            .ToDictionaryAsync(p => p.Id);

        var missing = productIds.Where(id => !productsDict.ContainsKey(id)).ToList();
        if (missing.Any())
            throw new Exception($"Produits introuvables : {string.Join(", ", missing)}");

        var groupedQuantities = dto.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, TotalQty = g.Sum(i => i.Quantity) })
            .ToDictionary(x => x.ProductId, x => x.TotalQty);

        foreach (var kv in groupedQuantities)
        {
            var product = productsDict[kv.Key];
            if (product.Stock < kv.Value)
                throw new Exception($"Stock insuffisant pour '{product.Name}'. Disponible : {product.Stock}, demandé : {kv.Value}");

            product.Stock -= kv.Value;
            _context.Products.Update(product);

            if (product.Stock <= product.LowStockThreshold)
            {
                await _notificationService.NotifyLowStockAsync(product.ShopId, product.Id, product.Stock);
            }
        }

        await _context.SaveChangesAsync();

        var groupedByShop = dto.Items
            .GroupBy(i => productsDict[i.ProductId].ShopId);

        var totalCart = dto.Items.Sum(i => productsDict[i.ProductId].Price * i.Quantity);

        decimal totalDiscount = 0;
        int? promotionId = null;

        if (!string.IsNullOrEmpty(dto.PromotionCode))
        {
            var result = await _promotionService.ValidateAndApplyAsync(dto.PromotionCode, totalCart);
            if (result.IsValid)
            {
                totalDiscount = result.DiscountAmount;
                promotionId = result.PromotionId;
                await _promotionService.IncrementUsageAsync(result.PromotionId.Value);
            }
            else
            {
                throw new Exception(result.Message);
            }
        }

        var orders = new List<Order>();
        var buyer = await _context.Users.FindAsync(buyerId);

        foreach (var group in groupedByShop)
        {
            var shopId = group.Key;
            var shop = await _context.Shops.FindAsync(shopId);
            if (shop == null) continue;

            var items = group.Select(i =>
            {
                var product = productsDict[i.ProductId];
                return new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = product.Price
                };
            }).ToList();

            var originalTotal = items.Sum(i => i.UnitPrice * i.Quantity);
            var shopDiscount = (totalCart > 0) ? (originalTotal / totalCart) * totalDiscount : 0;
            var discountedTotal = originalTotal - shopDiscount;

            var commission = discountedTotal * shop.CommissionRate;

            var order = new Order
            {
                BuyerId = buyerId,
                ShopId = shopId,
                Items = items,
                TotalAmount = discountedTotal,
                CommissionAmount = commission,
                VendorAmount = discountedTotal - commission,
                Status = "Pending",
                ShippingFullName = dto.ShippingFullName,
                ShippingAddress = dto.ShippingAddress,
                ShippingCity = dto.ShippingCity,
                ShippingPhone = dto.ShippingPhone,
                PaymentMethod = dto.PaymentMethod,
                PromotionId = promotionId,
                DiscountAmount = shopDiscount,
                PromotionCodeApplied = dto.PromotionCode
            };

            orders.Add(order);
        }

        _context.Orders.AddRange(orders);
        await _context.SaveChangesAsync();

        foreach (var order in orders)
        {
            await _notificationService.NotifyNewOrderAsync(order.ShopId, order.Id);
        }

        var allProducts = productsDict.Values.ToList();
        return orders.Select(o => MapToDto(o, allProducts, buyer)).ToList();
    }

    public async Task<List<OrderResponseDto>> GetBuyerOrdersAsync(int buyerId)
    {
        var orders = await _context.Orders
            .Include(o => o.Shop)
            .Include(o => o.Buyer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.BuyerId == buyerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(o => MapToDto(o, o.Items.Select(i => i.Product).ToList(), o.Buyer)).ToList();
    }

    public async Task<List<OrderResponseDto>> GetVendorOrdersAsync(int shopId)
    {
        var orders = await _context.Orders
            .Include(o => o.Shop)
            .Include(o => o.Buyer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.ShopId == shopId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(o => MapToDto(o, o.Items.Select(i => i.Product).ToList(), o.Buyer)).ToList();
    }

     public async Task<bool> UpdateStatusAsync(int orderId, string status, string? cancellationReason = null)
    {
        Console.WriteLine($"[DEBUG] UpdateStatusAsync - Commande {orderId}, Nouveau statut: '{status}'");

        var order = await _context.Orders
            .Include(o => o.Buyer)
            .Include(o => o.Shop)
                .ThenInclude(s => s.Vendor)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return false;

        var oldStatus = order.Status;
        order.Status = status;

        if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(cancellationReason))
        {
            order.CancellationReason = cancellationReason;
            Console.WriteLine($"[DEBUG] Raison d'annulation enregistrée : '{cancellationReason}'");
        }
        else if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[DEBUG] Annulation sans raison (cancellationReason est null ou vide)");
        }

        await _context.SaveChangesAsync();

        await _notificationService.NotifyOrderStatusChangeAsync(orderId, oldStatus, status);

        Console.WriteLine($"[DEBUG] Statut mis à jour de '{oldStatus}' à '{status}'");

        if (status.Equals("Shipped", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[DEBUG] Statut '{status}' déclenche l'envoi d'email.");
            var buyer = order.Buyer;
            if (buyer != null && !string.IsNullOrWhiteSpace(buyer.Email))
            {
                Console.WriteLine($"[DEBUG] Email de l'acheteur: {buyer.Email}");
                await SendStatusEmailAsync(order, status, cancellationReason);
                Console.WriteLine($"[DEBUG] Email envoyé avec succès.");
            }
            else
            {
                Console.WriteLine($"[DEBUG] Email non envoyé : acheteur sans email (Id={buyer?.Id})");
            }
        }
        else
        {
            Console.WriteLine($"[DEBUG] Statut '{status}' ne déclenche pas d'email.");
        }

        return true;
    }

    private async Task SendStatusEmailAsync(Order order, string status, string? reason)
    {
        try
        {
            var buyer = order.Buyer;
            if (buyer == null || string.IsNullOrWhiteSpace(buyer.Email))
            {
                Console.WriteLine($"[ERREUR] Email non envoyé : acheteur sans email (commande {order.Id})");
                return;
            }

            Console.WriteLine($"[DEBUG] Raison reçue dans SendStatusEmailAsync : '{reason ?? "null"}'");

            var shop = order.Shop;
            var items = order.Items.ToList();

            string subject = status.ToLower() switch
            {
                "shipped" => "Votre commande a été expédiée !",
                "delivered" => "Votre commande a été livrée !",
                "cancelled" => "Votre commande a été annulée",
                _ => "Mise à jour de votre commande"
            };

            string reasonHtml = "";
            if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(reason))
            {
                reasonHtml = $@"
                <div style='background-color:#f8d7da; border:1px solid #f5c6cb; padding:10px; margin:10px 0; border-radius:4px;'>
                    <strong>Raison :</strong> {reason}
                </div>";
            }

            var itemsHtml = string.Join("", items.Select(i => $@"
                <li style='margin-bottom:10px;'>
                    <img src='{(i.Product?.ImageUrl ?? "")}' alt='{i.Product?.Name}' style='width:50px;height:50px;object-fit:cover;vertical-align:middle;margin-right:10px;'/>
                    <strong>{i.Product?.Name}</strong> x {i.Quantity} – {(i.UnitPrice * i.Quantity):F2} AR
                </li>
            "));

            var htmlBody = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>{subject}</h2>
                <p>Bonjour {buyer.FullName},</p>
                <p>Votre commande n°{order.Id} est <strong>{TranslateStatus(status)}</strong>.</p>
                {reasonHtml}
                <hr/>
                <h3>Détails de la commande :</h3>
                <ul>
                    {itemsHtml}
                </ul>
                <p><strong>Total :</strong> {order.TotalAmount:F2} AR</p>
                <p><strong>Boutique :</strong> {shop?.Name}</p>
                <p><strong>Vendeur :</strong> {shop?.Vendor?.FullName}</p>
                <p><strong>Email du vendeur :</strong> {shop?.Vendor?.Email}</p>
                <hr/>
                <p>Merci de votre confiance.</p>
            </body>
            </html>
            ";

            Console.WriteLine($"[DEBUG] HTML de l'email : {htmlBody}");

            Console.WriteLine($"[DEBUG] Tentative d'envoi d'email à {buyer.Email}...");
            await _emailService.SendOrderStatusEmailAsync(buyer.Email, buyer.FullName, subject, htmlBody);
            Console.WriteLine($"[DEBUG] Email envoyé avec succès à {buyer.Email}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERREUR] Échec de l'envoi d'email pour la commande {order.Id} : {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    private string TranslateStatus(string status) => status.ToLower() switch
    {
        "pending" => "En attente",
        "confirmed" => "Confirmée",
        "shipped" => "Expédiée",
        "delivered" => "Livrée",
        "cancelled" => "Annulée",
        _ => status
    };

    private static OrderResponseDto MapToDto(Order o, List<Product> products, User? buyer)
    {
        return new OrderResponseDto
        {
            Id = o.Id,
            Status = o.Status,
            TotalAmount = o.TotalAmount,
            CommissionAmount = o.CommissionAmount,
            VendorAmount = o.VendorAmount,
            ShopName = o.Shop?.Name ?? "",
            CreatedAt = o.CreatedAt,
            BuyerName = buyer?.FullName ?? "",
            BuyerEmail = buyer?.Email ?? "",
            ShippingFullName = o.ShippingFullName,
            ShippingAddress = o.ShippingAddress,
            ShippingCity = o.ShippingCity,
            ShippingPhone = o.ShippingPhone,
            PaymentMethod = o.PaymentMethod,
            DiscountAmount = o.DiscountAmount,
            PromotionCodeApplied = o.PromotionCodeApplied,
            CancellationReason = o.CancellationReason,
            Items = o.Items.Select(i => new OrderItemResponseDto
            {
                OrderItemId = i.Id,
                ProductId = i.ProductId,
                ProductName = products.FirstOrDefault(p => p.Id == i.ProductId)?.Name ?? "",
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
    }
}