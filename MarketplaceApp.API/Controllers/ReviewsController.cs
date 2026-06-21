using MarketplaceApp.Application.DTOs.Review;
using MarketplaceApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MarketplaceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var reviews = await _reviewService.GetReviewsByProductIdAsync(productId);
        var average = await _reviewService.GetAverageRatingAsync(productId);

        return Ok(new
        {
            reviews,
            average,
            count = reviews.Count
        });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
    {
        try
        {
            var buyerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _reviewService.CreateReviewAsync(buyerId, dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("can-review/{productId}")]
    [Authorize]
    public async Task<IActionResult> CanReview(int productId)
    {
        var buyerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var can = await _reviewService.CanUserReviewAsync(buyerId, productId);
        return Ok(new { canReview = can });
    }

    [HttpGet("eligible-order-item/{productId}")]
    [Authorize]
    public async Task<IActionResult> GetEligibleOrderItem(int productId)
    {
        var buyerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var orderItemId = await _reviewService.GetEligibleOrderItemIdAsync(buyerId, productId);

        if (orderItemId == null)
            return NotFound(new { message = "Aucun article éligible." });

        return Ok(new { orderItemId });
    }
}