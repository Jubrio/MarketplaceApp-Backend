namespace MarketplaceApp.Application.DTOs.Order;

public class CreateOrderDto
{

    public List<OrderItemDto> Items { get; set; } = new();
    public string ShippingFullName { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "CashOnDelivery";
    public string? PromotionCode { get; set; }
}

public class OrderItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}