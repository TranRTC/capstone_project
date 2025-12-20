using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Core.Entities;

namespace IoTMonitoringSystem.Services
{
    public interface IDeviceService
    {
        Task<DeviceDto> GetDeviceByIdAsync(int deviceId);
        Task<List<DeviceListDto>> GetAllDevicesAsync();
        Task<DeviceDto> CreateDeviceAsync(CreateDeviceDto dto);
        Task<DeviceDto> UpdateDeviceAsync(int deviceId, UpdateDeviceDto dto);
        Task DeleteDeviceAsync(int deviceId);
        Task UpdateDeviceStatusAsync(int deviceId, string status, string? message = null);
        Task UpdateDeviceLastSeenAsync(int deviceId);
    }
}

