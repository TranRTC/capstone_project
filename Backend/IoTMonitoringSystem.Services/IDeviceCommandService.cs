using IoTMonitoringSystem.Core.DTOs;

namespace IoTMonitoringSystem.Services
{
    public interface IDeviceCommandService
    {
        Task<DeviceCommandDto> CreateCommandAsync(int deviceId, CreateDeviceCommandDto dto, string? requestedBy = null);
        Task<PagedResult<DeviceCommandDto>> GetCommandsByDeviceIdAsync(int deviceId, DeviceCommandQueryDto query);
        Task<DeviceCommandDto> GetCommandByIdAsync(long commandId);
        Task<DeviceCommandDto> UpdateCommandStatusAsync(long commandId, string status, string? errorMessage = null);
    }
}
