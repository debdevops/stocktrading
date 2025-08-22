using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Models;
using System.Security.Claims;
using Portfolio.API.DTOs;
using Portfolio.API.Services;

namespace Portfolio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PortfoliosController : ControllerBase
{
    private readonly IPortfolioService _portfolioService;

    public PortfoliosController(IPortfolioService portfolioService)
    {
        _portfolioService = portfolioService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PortfolioDto>>> CreatePortfolio([FromBody] CreatePortfolioDto portfolioDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var portfolio = await _portfolioService.CreatePortfolioAsync(userId, portfolioDto);
            return Ok(ApiResponse<PortfolioDto>.SuccessResponse(portfolio, "Portfolio created successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<PortfolioDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PortfolioDto>>>> GetPortfolios()
    {
        var userId = GetCurrentUserId();
        var portfolios = await _portfolioService.GetPortfoliosAsync(userId);
        return Ok(ApiResponse<List<PortfolioDto>>.SuccessResponse(portfolios));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PortfolioDto>>> GetPortfolio(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var portfolio = await _portfolioService.GetPortfolioAsync(userId, id);
            return Ok(ApiResponse<PortfolioDto>.SuccessResponse(portfolio));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<PortfolioDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<PortfolioDto>>> UpdatePortfolio(Guid id, [FromBody] CreatePortfolioDto portfolioDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var portfolio = await _portfolioService.UpdatePortfolioAsync(userId, id, portfolioDto);
            return Ok(ApiResponse<PortfolioDto>.SuccessResponse(portfolio, "Portfolio updated successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<PortfolioDto>.ErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<PortfolioDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeletePortfolio(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _portfolioService.DeletePortfolioAsync(userId, id);
            return Ok(ApiResponse<bool>.SuccessResponse(result, "Portfolio deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<PortfolioSummaryDto>>> GetPortfolioSummary()
    {
        var userId = GetCurrentUserId();
        var summary = await _portfolioService.GetPortfolioSummaryAsync(userId);
        return Ok(ApiResponse<PortfolioSummaryDto>.SuccessResponse(summary));
    }

    [HttpGet("{id}/analytics")]
    public async Task<ActionResult<ApiResponse<PortfolioAnalyticsDto>>> GetPortfolioAnalytics(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var analytics = await _portfolioService.GetPortfolioAnalyticsAsync(userId, id);
            return Ok(ApiResponse<PortfolioAnalyticsDto>.SuccessResponse(analytics));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<PortfolioAnalyticsDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("{id}/rebalance")]
    public async Task<ActionResult<ApiResponse<RebalanceRecommendationDto>>> GetRebalanceRecommendation(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var recommendation = await _portfolioService.GetRebalanceRecommendationAsync(userId, id);
            return Ok(ApiResponse<RebalanceRecommendationDto>.SuccessResponse(recommendation));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<RebalanceRecommendationDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("{id}/update-values")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdatePortfolioValues(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // Verify portfolio ownership
            await _portfolioService.GetPortfolioAsync(userId, id);
            
            await _portfolioService.UpdatePortfolioValuesAsync(id);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "Portfolio values updated successfully"));
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
