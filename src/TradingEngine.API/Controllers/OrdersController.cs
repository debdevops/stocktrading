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
public class OrdersController : ControllerBase
{
    private readonly ITradingService _tradingService;

    public OrdersController(ITradingService tradingService)
    {
        _tradingService = tradingService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrderExecutionResult>>> CreateOrder([FromBody] CreateOrderDto orderDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _tradingService.CreateOrderAsync(userId, orderDto);
            
            if (result.Success)
            {
                return Ok(ApiResponse<OrderExecutionResult>.SuccessResponse(result, "Order created successfully"));
            }
            else
            {
                return BadRequest(ApiResponse<OrderExecutionResult>.ErrorResponse(result.Message));
            }
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<OrderExecutionResult>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<OrderExecutionResult>.ErrorResponse("An error occurred while creating the order"));
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<OrderDto>>>> GetOrders([FromQuery] OrderFilterDto filter)
    {
        var userId = GetCurrentUserId();
        var orders = await _tradingService.GetOrdersAsync(userId, filter);
        return Ok(ApiResponse<List<OrderDto>>.SuccessResponse(orders));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrder(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var order = await _tradingService.GetOrderAsync(userId, id);
            return Ok(ApiResponse<OrderDto>.SuccessResponse(order));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<OrderDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelOrder(Guid id, [FromBody] CancelOrderDto? cancelDto = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _tradingService.CancelOrderAsync(userId, id, cancelDto?.Reason);
            return Ok(ApiResponse<bool>.SuccessResponse(result, "Order cancelled successfully"));
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
