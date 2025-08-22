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
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> CreateTransaction([FromBody] CreateTransactionDto transactionDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var transaction = await _transactionService.CreateTransactionAsync(userId, transactionDto);
            return Ok(ApiResponse<TransactionDto>.SuccessResponse(transaction, "Transaction created successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<TransactionDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<TransactionDto>.ErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<TransactionDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<TransactionDto>>>> GetTransactions([FromQuery] TransactionFilterDto filter)
    {
        var userId = GetCurrentUserId();
        var transactions = await _transactionService.GetTransactionsAsync(userId, filter);
        return Ok(ApiResponse<List<TransactionDto>>.SuccessResponse(transactions));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> GetTransaction(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var transaction = await _transactionService.GetTransactionAsync(userId, id);
            return Ok(ApiResponse<TransactionDto>.SuccessResponse(transaction));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<TransactionDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTransaction(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _transactionService.DeleteTransactionAsync(userId, id);
            return Ok(ApiResponse<bool>.SuccessResponse(result, "Transaction deleted successfully"));
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
