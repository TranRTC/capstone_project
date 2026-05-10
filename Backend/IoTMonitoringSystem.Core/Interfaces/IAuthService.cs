using IoTMonitoringSystem.Core.DTOs;

namespace IoTMonitoringSystem.Core.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
        Task<UserDto> CreateUserAsync(CreateUserDto dto);
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);
        Task<bool> DeactivateUserAsync(int userId);
        Task<UserDto?> UpdateUserRoleAsync(int userId, string role);
    }
}
