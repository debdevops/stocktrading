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
public class WatchlistsController : ControllerBase
{
    private readonly IWatchlistService _watchlistService;

    public WatchlistsController(IWatchlistService watchlistService)
    {
        _watchlistService = watchlistService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<WatchlistDto>>> CreateWatchlist([FromBody] CreateWatchlistDto watchlistDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var watchlist = await _watchlistService.CreateWatchlistAsync(userId, watchlistDto);
            return Ok(ApiResponse<WatchlistDto>.SuccessResponse(watchlist, "Watchlist created successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<WatchlistDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<WatchlistDto>>>> GetWatchlists()
    {
        var userId = GetCurrentUserId();
        var watchlists = await _watchlistService.GetWatchlistsAsync(userId);
        return Ok(ApiResponse<List<WatchlistDto>>.SuccessResponse(watchlists));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<WatchlistDto>>> GetWatchlist(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var watchlist = await _watchlistService.GetWatchlistAsync(userId, id);
            return Ok(ApiResponse<WatchlistDto>.SuccessResponse(watchlist));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<WatchlistDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<WatchlistDto>>> UpdateWatchlist(Guid id, [FromBody] CreateWatchlistDto watchlistDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var watchlist = await _watchlistService.UpdateWatchlistAsync(userId, id, watchlistDto);
            return Ok(ApiResponse<WatchlistDto>.SuccessResponse(watchlist, "Watchlist updated successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<WatchlistDto>.ErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<WatchlistDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteWatchlist(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _watchlistService.DeleteWatchlistAsync(userId, id);
            return Ok(ApiResponse<bool>.SuccessResponse(result, "Watchlist deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("{id}/items")]
    public async Task<ActionResult<ApiResponse<WatchlistDto>>> AddItemToWatchlist(Guid id, [FromBody] AddWatchlistItemDto itemDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var watchlist = await _watchlistService.AddItemToWatchlistAsync(userId, id, itemDto);
            return Ok(ApiResponse<WatchlistDto>.SuccessResponse(watchlist, "Item added to watchlist successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<WatchlistDto>.ErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<WatchlistDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("{id}/items/{symbol}")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveItemFromWatchlist(Guid id, string symbol)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _watchlistService.RemoveItemFromWatchlistAsync(userId, id, symbol);
            return Ok(ApiResponse<bool>.SuccessResponse(result, "Item removed from watchlist successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
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
