using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using IoTMonitoringSystem.Infrastructure.Data;

namespace IoTMonitoringSystem.Services
{
    public class SensorService : ISensorService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDeviceRepository _deviceRepository;

        public SensorService(ApplicationDbContext context, IDeviceRepository deviceRepository)
        {
            _context = context;
            _deviceRepository = deviceRepository;
        }

        public async Task<SensorDto> GetSensorByIdAsync(int sensorId)
        {
            var sensor = await _context.Sensors
                .Include(s => s.Device)
                .FirstOrDefaultAsync(s => s.SensorId == sensorId);

            if (sensor == null)
                throw new KeyNotFoundException($"Sensor with ID {sensorId} not found");

            return MapToSensorDto(sensor);
        }

        public async Task<List<SensorDto>> GetSensorsByDeviceIdAsync(int deviceId)
        {
            var sensors = await _context.Sensors
                .Where(s => s.DeviceId == deviceId)
                .ToListAsync();

            return sensors.Select(MapToSensorDto).ToList();
        }

        public async Task<SensorDto> CreateSensorAsync(int deviceId, CreateSensorDto dto)
        {
            var device = await _deviceRepository.GetByIdAsync(deviceId);
            if (device == null)
                throw new KeyNotFoundException($"Device with ID {deviceId} not found");

            var sensor = new Sensor
            {
                DeviceId = deviceId,
                EdgeDeviceId = dto.EdgeDeviceId,
                SensorName = dto.SensorName,
                SensorType = dto.SensorType,
                Unit = dto.Unit,
                MinValue = dto.MinValue,
                MaxValue = dto.MaxValue,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Sensors.AddAsync(sensor);
            await _context.SaveChangesAsync();

            return MapToSensorDto(sensor);
        }

        public async Task<SensorDto> UpdateSensorAsync(int sensorId, UpdateSensorDto dto)
        {
            var sensor = await _context.Sensors.FindAsync(sensorId);
            if (sensor == null)
                throw new KeyNotFoundException($"Sensor with ID {sensorId} not found");

            if (dto.SensorName != null)
                sensor.SensorName = dto.SensorName;
            if (dto.SensorType != null)
                sensor.SensorType = dto.SensorType;
            if (dto.Unit != null)
                sensor.Unit = dto.Unit;
            if (dto.MinValue.HasValue)
                sensor.MinValue = dto.MinValue;
            if (dto.MaxValue.HasValue)
                sensor.MaxValue = dto.MaxValue;
            if (dto.IsActive.HasValue)
                sensor.IsActive = dto.IsActive.Value;

            sensor.UpdatedAt = DateTime.UtcNow;

            _context.Sensors.Update(sensor);
            await _context.SaveChangesAsync();

            return MapToSensorDto(sensor);
        }

        public async Task DeleteSensorAsync(int sensorId)
        {
            var sensor = await _context.Sensors.FindAsync(sensorId);
            if (sensor == null)
                throw new KeyNotFoundException($"Sensor with ID {sensorId} not found");

            _context.Sensors.Remove(sensor);
            await _context.SaveChangesAsync();
        }

        private SensorDto MapToSensorDto(Sensor sensor)
        {
            return new SensorDto
            {
                SensorId = sensor.SensorId,
                DeviceId = sensor.DeviceId,
                EdgeDeviceId = sensor.EdgeDeviceId,
                SensorName = sensor.SensorName,
                SensorType = sensor.SensorType,
                Unit = sensor.Unit,
                MinValue = sensor.MinValue,
                MaxValue = sensor.MaxValue,
                IsActive = sensor.IsActive,
                CreatedAt = sensor.CreatedAt,
                UpdatedAt = sensor.UpdatedAt
            };
        }
    }
}

