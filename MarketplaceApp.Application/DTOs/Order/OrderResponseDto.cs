using System;
using System.Collections.Generic;

namespace MarketplaceApp.Application.DTOs.Order;

public class OrderResponseDto
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal VendorAmount { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerEmail { get; set; } = string.Empty;
    public string ShippingFullName { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;

    public decimal? DiscountAmount { get; set; }
    public string? PromotionCodeApplied { get; set; }


    public List<OrderItemResponseDto> Items { get; set; } = new();
}

public class OrderItemResponseDto
{
    public int OrderItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}