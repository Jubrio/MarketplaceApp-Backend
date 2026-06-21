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

    public OrderService(AppDbContext context, INotificationService notificationService, IPromotionService promotionService)
    {
        _context = context;
        _notificationService = notificationService;
        _promotionService = promotionService;
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

    public async Task<bool> UpdateStatusAsync(int orderId, string status)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return false;

        var oldStatus = order.Status;
        order.Status = status;
        await _context.SaveChangesAsync();
        await _notificationService.NotifyOrderStatusChangeAsync(orderId, oldStatus, status);
        return true;
    }

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