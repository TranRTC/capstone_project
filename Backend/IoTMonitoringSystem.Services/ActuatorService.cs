using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Infrastructure.Data;
using IoTMonitoringSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IoTMonitoringSystem.Services
{
    public class ActuatorService : IActuatorService
    {
        private static readonly HashSet<string> ValidKinds = new(StringComparer.OrdinalIgnoreCase)
        {
            "Discrete",
            "Analog"
        };

        private readonly ApplicationDbContext _context;
        private readonly IDeviceRepository _deviceRepository;
        private readonly IActuatorRepository _actuatorRepository;

        public ActuatorService(
            ApplicationDbContext context,
            IDeviceRepository deviceRepository,
            IActuatorRepository actuatorRepository)
        {
            _context = context;
            _deviceRepository = deviceRepository;
            _actuatorRepository = actuatorRepository;
        }

        public async Task<List<ActuatorDto>> GetByDeviceIdAsync(int deviceId)
        {
            await EnsureDeviceExists(deviceId);
            var items = await _actuatorRepository.GetByDeviceIdAsync(deviceId);
            return items.Select(MapToDto).ToList();
        }

        public async Task<ActuatorDto> GetByIdAsync(int deviceId, int actuatorId)
        {
            await EnsureDeviceExists(deviceId);
            var actuator = await _actuatorRepository.GetByIdAndDeviceAsync(actuatorId, deviceId);
            if (actuator == null)
                throw new KeyNotFoundException($"Actuator with ID {actuatorId} not found for device {deviceId}");

            return MapToDto(actuator);
        }

        public async Task<ActuatorDto> CreateAsync(int deviceId, CreateActuatorDto dto)
        {
            await EnsureDeviceExists(deviceId);
            ValidateKind(dto.Kind);

            if (dto.FeedbackSensorId.HasValue)
                await ValidateFeedbackSensor(deviceId, dto.FeedbackSensorId.Value);

            var actuator = new Actuator
            {
                DeviceId = deviceId,
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Kind = NormalizeKind(dto.Kind),
                Channel = dto.Channel?.Trim(),
                AnalogMin = dto.AnalogMin,
                AnalogMax = dto.AnalogMax,
                ControlUnit = dto.ControlUnit?.Trim(),
                FeedbackSensorId = dto.FeedbackSensorId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            ValidateAnalogRange(actuator);

            await _context.Actuators.AddAsync(actuator);
            await _context.SaveChangesAsync();

            return MapToDto(actuator);
        }

        public async Task<ActuatorDto> UpdateAsync(int deviceId, int actuatorId, UpdateActuatorDto dto)
        {
            var actuator = await _actuatorRepository.GetByIdAndDeviceAsync(actuatorId, deviceId);
            if (actuator == null)
                throw new KeyNotFoundException($"Actuator with ID {actuatorId} not found for device {deviceId}");

            if (dto.Name != null)
                actuator.Name = dto.Name.Trim();
            if (dto.Description != null)
                actuator.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            if (dto.Kind != null)
            {
                ValidateKind(dto.Kind);
                actuator.Kind = NormalizeKind(dto.Kind);
            }

            if (dto.Channel != null)
                actuator.Channel = string.IsNullOrWhiteSpace(dto.Channel) ? null : dto.Channel.Trim();
            if (dto.AnalogMin.HasValue)
                actuator.AnalogMin = dto.AnalogMin;
            if (dto.AnalogMax.HasValue)
                actuator.AnalogMax = dto.AnalogMax;
            if (dto.ControlUnit != null)
                actuator.ControlUnit = string.IsNullOrWhiteSpace(dto.ControlUnit) ? null : dto.ControlUnit.Trim();
            if (dto.IsActive.HasValue)
                actuator.IsActive = dto.IsActive.Value;

            if (dto.FeedbackSensorId.HasValue)
            {
                var sid = dto.FeedbackSensorId.Value;
                if (sid == 0)
                    actuator.FeedbackSensorId = null;
                else
                {
                    await ValidateFeedbackSensor(deviceId, sid);
                    actuator.FeedbackSensorId = sid;
                }
            }

            ValidateAnalogRange(actuator);

            actuator.UpdatedAt = DateTime.UtcNow;
            _context.Actuators.Update(actuator);
            await _context.SaveChangesAsync();

            return MapToDto(actuator);
        }

        public async Task DeleteAsync(int deviceId, int actuatorId)
        {
            var actuator = await _actuatorRepository.GetByIdAndDeviceAsync(actuatorId, deviceId);
            if (actuator == null)
                throw new KeyNotFoundException($"Actuator with ID {actuatorId} not found for device {deviceId}");

            _context.Actuators.Remove(actuator);
            await _context.SaveChangesAsync();
        }

        private async Task EnsureDeviceExists(int deviceId)
        {
            var device = await _deviceRepository.GetByIdAsync(deviceId);
            if (device == null)
                throw new KeyNotFoundException($"Device with ID {deviceId} not found");
        }

        private async Task ValidateFeedbackSensor(int deviceId, int sensorId)
        {
            var sensor = await _context.Sensors.FirstOrDefaultAsync(s => s.SensorId == sensorId);
            if (sensor == null)
                throw new ArgumentException($"Sensor with ID {sensorId} not found");

            if (sensor.DeviceId != deviceId)
                throw new ArgumentException("Feedback sensor must belong to the same device as the actuator");
        }

        private static void ValidateKind(string kind)
        {
            if (!ValidKinds.Contains(kind.Trim()))
                throw new ArgumentException($"Kind must be Discrete or Analog, got '{kind}'");
        }

        private static string NormalizeKind(string kind) =>
            ValidKinds.First(k => string.Equals(k, kind.Trim(), StringComparison.OrdinalIgnoreCase));

        private static void ValidateAnalogRange(Actuator actuator)
        {
            if (!actuator.AnalogMin.HasValue || !actuator.AnalogMax.HasValue)
                return;

            if (actuator.AnalogMin.Value > actuator.AnalogMax.Value)
                throw new ArgumentException("AnalogMin cannot be greater than AnalogMax");
        }

        private static ActuatorDto MapToDto(Actuator a)
        {
            return new ActuatorDto
            {
                ActuatorId = a.ActuatorId,
                DeviceId = a.DeviceId,
                Name = a.Name,
                Description = a.Description,
                Kind = a.Kind,
                Channel = a.Channel,
                AnalogMin = a.AnalogMin,
                AnalogMax = a.AnalogMax,
                ControlUnit = a.ControlUnit,
                IsActive = a.IsActive,
                FeedbackSensorId = a.FeedbackSensorId,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            };
        }
    }
}
