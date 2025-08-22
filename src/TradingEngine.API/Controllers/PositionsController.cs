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
public class PositionsController : ControllerBase
{
    private readonly ITradingService _tradingService;

    public PositionsController(ITradingService tradingService)
    {
        _tradingService = tradingService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PositionDto>>>> GetPositions()
    {
        var userId = GetCurrentUserId();
        var positions = await _tradingService.GetPositionsAsync(userId);
        return Ok(ApiResponse<List<PositionDto>>.SuccessResponse(positions));
    }

    [HttpGet("{symbol}")]
    public async Task<ActionResult<ApiResponse<PositionDto>>> GetPosition(string symbol)
    {
        var userId = GetCurrentUserId();
        var position = await _tradingService.GetPositionAsync(userId, symbol);
        
        if (position == null)
        {
            return NotFound(ApiResponse<PositionDto>.ErrorResponse("Position not found"));
        }

        return Ok(ApiResponse<PositionDto>.SuccessResponse(position));
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
