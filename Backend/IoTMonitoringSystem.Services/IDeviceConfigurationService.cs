using IoTMonitoringSystem.Core.DTOs;

namespace IoTMonitoringSystem.Services
{
    public interface IDeviceConfigurationService
    {
        Task<List<DeviceConfigurationDto>> GetByDeviceIdAsync(int deviceId);
        Task<List<DeviceConfigurationDto>> UpsertByDeviceIdAsync(int deviceId, UpsertDeviceConfigurationsDto dto);
    }
}
