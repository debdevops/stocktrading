using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Models;
using UserManagement.API.DTOs;
using UserManagement.API.Services;

namespace UserManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterUserDto registerDto)
    {
        try
        {
            var result = await _userService.RegisterAsync(registerDto);
            return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result, "User registered successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<AuthResponseDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            var result = await _userService.LoginAsync(loginDto);
            return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result, "Login successful"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<AuthResponseDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
    {
        try
        {
            var result = await _userService.RefreshTokenAsync(refreshTokenDto.RefreshToken);
            return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result, "Token refreshed successfully"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<AuthResponseDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("verify-email/{userId}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> VerifyEmail(Guid userId)
    {
        try
        {
            var result = await _userService.VerifyEmailAsync(userId);
            return Ok(ApiResponse<bool>.SuccessResponse(result, "Email verified successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }
}
