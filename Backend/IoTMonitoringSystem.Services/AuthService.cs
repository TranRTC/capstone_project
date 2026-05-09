using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Core.Interfaces;
using IoTMonitoringSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace IoTMonitoringSystem.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return null;

            var token = GenerateJwtToken(user);
            var expiry = DateTime.UtcNow.AddHours(GetTokenExpiryHours());

            return new LoginResponseDto
            {
                Token = token,
                Username = user.Username,
                Role = user.Role,
                ExpiresAt = expiry.ToString("o")
            };
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                throw new InvalidOperationException($"Username '{dto.Username}' is already taken.");

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return MapToDto(user);
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users.OrderBy(u => u.Username).ToListAsync();
            return users.Select(MapToDto);
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT key is not configured.");
            var issuer = _configuration["Jwt:Issuer"] ?? "IoTMonitoringSystem";
            var audience = _configuration["Jwt:Audience"] ?? "IoTMonitoringFrontend";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(GetTokenExpiryHours()),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private double GetTokenExpiryHours()
        {
            if (double.TryParse(_configuration["Jwt:ExpiryHours"], out var hours))
                return hours;
            return 8;
        }

        private static UserDto MapToDto(User user) => new()
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}
