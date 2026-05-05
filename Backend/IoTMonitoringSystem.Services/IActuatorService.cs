using IoTMonitoringSystem.Core.DTOs;

namespace IoTMonitoringSystem.Services
{
    public interface IActuatorService
    {
        Task<List<ActuatorDto>> GetByDeviceIdAsync(int deviceId);
        Task<ActuatorDto> GetByIdAsync(int deviceId, int actuatorId);
        Task<ActuatorDto> CreateAsync(int deviceId, CreateActuatorDto dto);
        Task<ActuatorDto> UpdateAsync(int deviceId, int actuatorId, UpdateActuatorDto dto);
        Task DeleteAsync(int deviceId, int actuatorId);
    }
}
