using AutoMapper;
using Microsoft.EntityFrameworkCore;
using UserManagement.API.Data;
using UserManagement.API.DTOs;
using UserManagement.API.Models;
using Shared.Common.Services;
using BCrypt.Net;

namespace UserManagement.API.Services;

public interface IUserService
{
    Task<AuthResponseDto> RegisterAsync(RegisterUserDto registerDto);
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    Task<UserDto> GetUserByIdAsync(Guid userId);
    Task<UserDto> UpdateUserProfileAsync(Guid userId, UpdateUserProfileDto profileDto);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto);
    Task<bool> VerifyEmailAsync(Guid userId);
    Task<List<UserDto>> GetUsersAsync(int page = 1, int pageSize = 10);
}

public class UserService : IUserService
{
    private readonly UserDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IMapper _mapper;

    public UserService(UserDbContext context, IJwtService jwtService, IMapper mapper)
    {
        _context = context;
        _jwtService = jwtService;
        _mapper = mapper;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto registerDto)
    {
        // Check if user already exists
        if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
        {
            throw new ArgumentException("User with this email already exists");
        }

        // Create new user
        var user = new User
        {
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            Email = registerDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
            PhoneNumber = registerDto.PhoneNumber,
            DateOfBirth = registerDto.DateOfBirth,
            IsActive = true
        };

        _context.Users.Add(user);

        // Assign default "Trader" role
        var traderRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Trader");
        if (traderRole != null)
        {
            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = traderRole.Id
            };
            _context.UserRoles.Add(userRole);
        }

        // Create default profile
        var profile = new UserProfile
        {
            UserId = user.Id,
            InitialBalance = 10000, // Default virtual balance
            RiskTolerance = "Moderate",
            TradingExperience = "Beginner",
            NotificationsEnabled = true
        };
        _context.UserProfiles.Add(profile);

        await _context.SaveChangesAsync();

        // Generate tokens
        var roles = await GetUserRolesAsync(user.Id);
        var accessToken = _jwtService.GenerateToken(user.Id.ToString(), user.Email, roles);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Update user with refresh token
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        var userDto = await GetUserByIdAsync(user.Id);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = userDto
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account is deactivated");
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;

        // Generate tokens
        var roles = await GetUserRolesAsync(user.Id);
        var accessToken = _jwtService.GenerateToken(user.Id.ToString(), user.Email, roles);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Update refresh token
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        var userDto = await GetUserByIdAsync(user.Id);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = userDto
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken && 
                                    u.RefreshTokenExpiryTime > DateTime.UtcNow);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var roles = await GetUserRolesAsync(user.Id);
        var accessToken = _jwtService.GenerateToken(user.Id.ToString(), user.Email, roles);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        var userDto = await GetUserByIdAsync(user.Id);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = userDto
        };
    }

    public async Task<UserDto> GetUserByIdAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        var userDto = _mapper.Map<UserDto>(user);
        userDto.Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        return userDto;
    }

    public async Task<UserDto> UpdateUserProfileAsync(Guid userId, UpdateUserProfileDto profileDto)
    {
        var user = await _context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        if (user.Profile == null)
        {
            user.Profile = new UserProfile { UserId = userId };
            _context.UserProfiles.Add(user.Profile);
        }

        _mapper.Map(profileDto, user.Profile);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetUserByIdAsync(userId);
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Current password is incorrect");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> VerifyEmailAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        user.IsEmailVerified = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<UserDto>> GetUsersAsync(int page = 1, int pageSize = 10)
    {
        var users = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Include(u => u.Profile)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return users.Select(u =>
        {
            var userDto = _mapper.Map<UserDto>(u);
            userDto.Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList();
            return userDto;
        }).ToList();
    }

    private async Task<List<string>> GetUserRolesAsync(Guid userId)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync();
    }
}
