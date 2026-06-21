using MarketplaceApp.Application.DTOs.Payment;
using MarketplaceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("simulate")]
    public async Task<IActionResult> SimulatePayment([FromBody] SimulatePaymentDto dto)
    {
        try
        {
            var result = await _paymentService.SimulatePaymentAsync(dto);
            if (!result) return BadRequest(new { message = "Aucune commande à payer." });

            return Ok(new { message = "Paiement simulé avec succès." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}