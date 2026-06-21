namespace MarketplaceApp.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public int BuyerId { get; set; }
    public int ShopId { get; set; }
    public string Status { get; set; } = "Pending";
    public decimal TotalAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal VendorAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User Buyer { get; set; } = null!;
    public Shop Shop { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public Payment? Payment { get; set; }

    public string ShippingFullName { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "CashOnDelivery"; 
    public int? PromotionId { get; set; }
    public decimal? DiscountAmount { get; set; }
    public string? PromotionCodeApplied { get; set; }
    public Promotion? Promotion { get; set; }

}