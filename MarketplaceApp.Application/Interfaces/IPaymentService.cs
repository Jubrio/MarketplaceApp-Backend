using MarketplaceApp.Application.DTOs.Payment;

namespace MarketplaceApp.Application.Interfaces;

public interface IPaymentService
{
    Task<bool> SimulatePaymentAsync(SimulatePaymentDto dto);
}