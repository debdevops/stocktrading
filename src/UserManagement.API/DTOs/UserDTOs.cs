using System.ComponentModel.DataAnnotations;

namespace UserManagement.API.DTOs;

public class RegisterUserDto
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
    
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
}

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UserDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
    public UserProfileDto? Profile { get; set; }
}

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public decimal? InitialBalance { get; set; }
    public string? RiskTolerance { get; set; }
    public string? TradingExperience { get; set; }
    public bool NotificationsEnabled { get; set; }
    public bool TwoFactorEnabled { get; set; }
}

public class UpdateUserProfileDto
{
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public decimal? InitialBalance { get; set; }
    public string? RiskTolerance { get; set; }
    public string? TradingExperience { get; set; }
    public bool NotificationsEnabled { get; set; }
    public bool TwoFactorEnabled { get; set; }
}

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}

public class RefreshTokenDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;
    
    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
    
    [Required]
    [Compare("NewPassword")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
