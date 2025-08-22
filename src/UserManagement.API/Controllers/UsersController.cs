using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Models;
using System.Security.Claims;
using UserManagement.API.DTOs;
using UserManagement.API.Services;

namespace UserManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _userService.GetUserByIdAsync(userId);
            return Ok(ApiResponse<UserDto>.SuccessResponse(user));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<UserDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(Guid id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            return Ok(ApiResponse<UserDto>.SuccessResponse(user));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<UserDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var users = await _userService.GetUsersAsync(page, pageSize);
        return Ok(ApiResponse<List<UserDto>>.SuccessResponse(users));
    }

    [HttpPut("profile")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateProfile([FromBody] UpdateUserProfileDto profileDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _userService.UpdateUserProfileAsync(userId, profileDto);
            return Ok(ApiResponse<UserDto>.SuccessResponse(user, "Profile updated successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<UserDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _userService.ChangePasswordAsync(userId, changePasswordDto);
            return Ok(ApiResponse<bool>.SuccessResponse(result, "Password changed successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
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
