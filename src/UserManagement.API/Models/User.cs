using Shared.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace UserManagement.API.Models;

public class User : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
    
    public DateTime? DateOfBirth { get; set; }
    
    public bool IsEmailVerified { get; set; } = false;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime? LastLoginAt { get; set; }
    
    public string? RefreshToken { get; set; }
    
    public DateTime? RefreshTokenExpiryTime { get; set; }
    
    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual UserProfile? Profile { get; set; }
}

public class Role : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? Description { get; set; }
    
    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public class UserRole : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
}

public class UserProfile : BaseEntity
{
    public Guid UserId { get; set; }
    
    [MaxLength(500)]
    public string? Bio { get; set; }
    
    public string? ProfileImageUrl { get; set; }
    
    [MaxLength(100)]
    public string? Country { get; set; }
    
    [MaxLength(100)]
    public string? City { get; set; }
    
    public decimal? InitialBalance { get; set; }
    
    public string? RiskTolerance { get; set; } // Conservative, Moderate, Aggressive
    
    public string? TradingExperience { get; set; } // Beginner, Intermediate, Advanced
    
    public bool NotificationsEnabled { get; set; } = true;
    
    public bool TwoFactorEnabled { get; set; } = false;
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
}
