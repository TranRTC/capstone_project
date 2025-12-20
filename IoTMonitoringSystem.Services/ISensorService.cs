using IoTMonitoringSystem.Core.DTOs;

namespace IoTMonitoringSystem.Services
{
    public interface ISensorService
    {
        Task<SensorDto> GetSensorByIdAsync(int sensorId);
        Task<List<SensorDto>> GetSensorsByDeviceIdAsync(int deviceId);
        Task<SensorDto> CreateSensorAsync(int deviceId, CreateSensorDto dto);
        Task<SensorDto> UpdateSensorAsync(int sensorId, UpdateSensorDto dto);
        Task DeleteSensorAsync(int sensorId);
    }
}

