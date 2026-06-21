namespace MarketplaceApp.Domain.Entities;

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Provider { get; set; } = "Stripe";
    public string Status { get; set; } = "Pending";
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;

    public Order Order { get; set; } = null!;
}