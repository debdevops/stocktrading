using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Models;
using System.Security.Claims;
using TradingEngine.API.DTOs;
using TradingEngine.API.Services;

namespace TradingEngine.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TradesController : ControllerBase
{
    private readonly ITradingService _tradingService;

    public TradesController(ITradingService tradingService)
    {
        _tradingService = tradingService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<TradeDto>>>> GetTrades([FromQuery] TradeFilterDto filter)
    {
        var userId = GetCurrentUserId();
        var trades = await _tradingService.GetTradesAsync(userId, filter);
        return Ok(ApiResponse<List<TradeDto>>.SuccessResponse(trades));
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }
        return userId;
    }
}
