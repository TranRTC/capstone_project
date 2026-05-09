using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Core.Interfaces;

namespace IoTMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>Login — returns JWT token. No auth required.</summary>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(request);
            if (result == null)
                return Unauthorized(new { message = "Invalid username or password." });

            return Ok(result);
        }

        /// <summary>Get all users. Admin only.</summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _authService.GetAllUsersAsync();
            return Ok(users);
        }

        /// <summary>Create a new user. Admin only.</summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var user = await _authService.CreateUserAsync(dto);
                return CreatedAtAction(nameof(GetUsers), new { }, user);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>Change own password. Any authenticated user.</summary>
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("sub");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            var ok = await _authService.ChangePasswordAsync(userId, dto);
            if (!ok)
                return BadRequest(new { message = "Current password is incorrect." });

            return Ok(new { message = "Password changed successfully." });
        }

        /// <summary>Deactivate a user. Admin only.</summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeactivateUser(int userId)
        {
            var ok = await _authService.DeactivateUserAsync(userId);
            if (!ok)
                return NotFound();

            return Ok(new { message = "User deactivated." });
        }
    }
}
