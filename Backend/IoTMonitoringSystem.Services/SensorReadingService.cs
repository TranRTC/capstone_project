using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Core.Interfaces;
using IoTMonitoringSystem.Infrastructure.Data;
using IoTMonitoringSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IoTMonitoringSystem.Services
{
    public class SensorReadingService : ISensorReadingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISensorReadingRepository _readingRepository;
        private readonly IDeviceRepository _deviceRepository;
        private readonly IAlertService? _alertService;
        private readonly INotificationService? _notificationService;

        public SensorReadingService(
            ApplicationDbContext context,
            ISensorReadingRepository readingRepository,
            IDeviceRepository deviceRepository,
            IAlertService? alertService = null,
            INotificationService? notificationService = null)
        {
            _context = context;
            _readingRepository = readingRepository;
            _deviceRepository = deviceRepository;
            _alertService = alertService;
            _notificationService = notificationService;
        }

        public async Task<SensorReadingDto> CreateReadingAsync(CreateSensorReadingDto dto)
        {
            // Verify device exists
            var device = await _deviceRepository.GetByIdAsync(dto.DeviceId);
            if (device == null)
                throw new KeyNotFoundException($"Device with ID {dto.DeviceId} not found");

            // Verify sensor exists
            var sensor = await _context.Sensors.FindAsync(dto.SensorId);
            if (sensor == null)
                throw new KeyNotFoundException($"Sensor with ID {dto.SensorId} not found");

            var reading = new SensorReading
            {
                DeviceId = dto.DeviceId,
                SensorId = dto.SensorId,
                Value = dto.Value,
                Timestamp = dto.Timestamp ?? DateTime.UtcNow,
                Status = dto.Status,
                Quality = dto.Quality,
                CreatedAt = DateTime.UtcNow
            };

            var createdReading = await _readingRepository.CreateAsync(reading);
            var readingDto = MapToSensorReadingDto(createdReading);

            // Update device last seen
            await UpdateDeviceLastSeen(dto.DeviceId);

            // Evaluate alert rules
            if (_alertService != null)
                await _alertService.EvaluateAlertRulesAsync(readingDto);

            // Notify via SignalR
            if (_notificationService != null)
                await _notificationService.NotifySensorReadingAsync(readingDto);

            return readingDto;
        }

        public async Task<List<SensorReadingDto>> CreateReadingsBatchAsync(List<CreateSensorReadingDto> dtos)
        {
            var readings = new List<SensorReading>();
            var deviceIds = new HashSet<int>();

            foreach (var dto in dtos)
            {
                var reading = new SensorReading
                {
                    DeviceId = dto.DeviceId,
                    SensorId = dto.SensorId,
                    Value = dto.Value,
                    Timestamp = dto.Timestamp ?? DateTime.UtcNow,
                    Status = dto.Status,
                    Quality = dto.Quality,
                    CreatedAt = DateTime.UtcNow
                };

                readings.Add(reading);
                deviceIds.Add(dto.DeviceId);
            }

            await _context.SensorReadings.AddRangeAsync(readings);
            await _context.SaveChangesAsync();

            var readingDtos = readings.Select(MapToSensorReadingDto).ToList();

            // Update last seen for all devices
            foreach (var deviceId in deviceIds)
            {
                await UpdateDeviceLastSeen(deviceId);
            }

            // Evaluate alert rules and notify for each reading
            if (_alertService != null && _notificationService != null)
            {
                foreach (var readingDto in readingDtos)
                {
                    await _alertService.EvaluateAlertRulesAsync(readingDto);
                    await _notificationService.NotifySensorReadingAsync(readingDto);
                }
            }

            return readingDtos;
        }

        public async Task<PagedResult<SensorReadingDto>> GetReadingsAsync(SensorReadingQueryDto query)
        {
            var dbQuery = _context.SensorReadings.AsQueryable();

            if (query.DeviceId.HasValue)
                dbQuery = dbQuery.Where(sr => sr.DeviceId == query.DeviceId.Value);

            if (query.SensorId.HasValue)
                dbQuery = dbQuery.Where(sr => sr.SensorId == query.SensorId.Value);

            if (query.StartDate.HasValue)
                dbQuery = dbQuery.Where(sr => sr.Timestamp >= query.StartDate.Value);

            if (query.EndDate.HasValue)
                dbQuery = dbQuery.Where(sr => sr.Timestamp <= query.EndDate.Value);

            var totalCount = await dbQuery.CountAsync();

            var items = await dbQuery
                .OrderByDescending(sr => sr.Timestamp)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<SensorReadingDto>
            {
                Items = items.Select(MapToSensorReadingDto).ToList(),
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }

        public async Task<List<SensorReadingDto>> GetReadingsByDeviceIdAsync(int deviceId, DateTime? startDate, DateTime? endDate)
        {
            var readings = await _readingRepository.GetByDeviceIdAsync(deviceId, startDate, endDate);
            return readings.Select(MapToSensorReadingDto).ToList();
        }

        public async Task<List<SensorReadingDto>> GetReadingsBySensorIdAsync(int sensorId, DateTime? startDate, DateTime? endDate)
        {
            var readings = await _readingRepository.GetBySensorIdAsync(sensorId, startDate, endDate);
            return readings.Select(MapToSensorReadingDto).ToList();
        }

        private async Task UpdateDeviceLastSeen(int deviceId)
        {
            var device = await _deviceRepository.GetByIdAsync(deviceId);
            if (device != null)
            {
                device.LastSeenAt = DateTime.UtcNow;
                device.UpdatedAt = DateTime.UtcNow;
                await _deviceRepository.UpdateAsync(device);
            }
        }

        private SensorReadingDto MapToSensorReadingDto(SensorReading reading)
        {
            return new SensorReadingDto
            {
                ReadingId = reading.ReadingId,
                DeviceId = reading.DeviceId,
                SensorId = reading.SensorId,
                Value = reading.Value,
                Timestamp = reading.Timestamp,
                Status = reading.Status,
                Quality = reading.Quality,
                CreatedAt = reading.CreatedAt
            };
        }
    }
}

