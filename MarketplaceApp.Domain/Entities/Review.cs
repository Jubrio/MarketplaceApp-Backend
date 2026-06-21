namespace MarketplaceApp.Domain.Entities;

public class Review
{
    public int Id { get; set; }
    public int OrderItemId { get; set; }
    public int BuyerId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public OrderItem OrderItem { get; set; } = null!;
    public User Buyer { get; set; } = null!;
}