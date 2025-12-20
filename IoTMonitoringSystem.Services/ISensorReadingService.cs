using IoTMonitoringSystem.Core.DTOs;

namespace IoTMonitoringSystem.Services
{
    public interface ISensorReadingService
    {
        Task<SensorReadingDto> CreateReadingAsync(CreateSensorReadingDto dto);
        Task<List<SensorReadingDto>> CreateReadingsBatchAsync(List<CreateSensorReadingDto> dtos);
        Task<PagedResult<SensorReadingDto>> GetReadingsAsync(SensorReadingQueryDto query);
        Task<List<SensorReadingDto>> GetReadingsByDeviceIdAsync(int deviceId, DateTime? startDate, DateTime? endDate);
        Task<List<SensorReadingDto>> GetReadingsBySensorIdAsync(int sensorId, DateTime? startDate, DateTime? endDate);
    }
}

