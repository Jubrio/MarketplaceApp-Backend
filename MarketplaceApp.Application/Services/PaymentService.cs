using MarketplaceApp.Application.DTOs.Payment;
using MarketplaceApp.Application.Interfaces;
using MarketplaceApp.Domain.Entities;
using MarketplaceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceApp.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _context;

    public PaymentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> SimulatePaymentAsync(SimulatePaymentDto dto)
    {
        if (dto.OrderIds == null || !dto.OrderIds.Any())
            return false;

        var orders = await _context.Orders
            .Where(o => dto.OrderIds.Contains(o.Id))
            .ToListAsync();

        if (!orders.Any()) return false;

        var reference = $"SIM-{DateTime.UtcNow:yyyyMMddHHmmss}";

        foreach (var order in orders)
        {
            order.Status = "Confirmed";

            var payment = new Payment
            {
                OrderId = order.Id,
                Reference = $"{reference}-{order.Id}",
                Amount = order.TotalAmount,
                Provider = "Simulation",
                Status = "Completed",
                PaidAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
        }

        await _context.SaveChangesAsync();
        return true;
    }
}