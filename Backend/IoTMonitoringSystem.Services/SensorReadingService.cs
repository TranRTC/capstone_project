using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Core.Interfaces;
using IoTMonitoringSystem.Infrastructure.Data;
using IoTMonitoringSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IoTMonitoringSystem.Services
{
    // Ingests sensor readings (single/batch), queries history with filters/paging, updates device LastSeenAt,
    // evaluates alert rules, and pushes SensorReadingDto to SignalR when INotificationService is registered.
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

        // Validates device and sensor, persists reading, maps to output DTO, updates last seen, evaluates alerts, notifies clients.
        public async Task<SensorReadingDto> CreateReadingAsync(CreateSensorReadingDto dto)
        {
            var device = await _deviceRepository.GetByIdAsync(dto.DeviceId);
            if (device == null)
                throw new KeyNotFoundException($"Device with ID {dto.DeviceId} not found");

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

            await UpdateDeviceLastSeen(dto.DeviceId);

            if (_alertService != null)
                await _alertService.EvaluateAlertRulesAsync(readingDto);

            if (_notificationService != null)
                await _notificationService.NotifySensorReadingAsync(readingDto);

            return readingDto;
        }

        // Bulk insert via DbContext (one SaveChanges). Then last-seen, per-reading alert eval + SignalR only if both services are non-null.
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

            foreach (var deviceId in deviceIds)
            {
                await UpdateDeviceLastSeen(deviceId);
            }

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

        // Paged list: optional device/sensor/time filters, newest first.
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

        // All readings for one device in optional time range (via repository).
        public async Task<List<SensorReadingDto>> GetReadingsByDeviceIdAsync(int deviceId, DateTime? startDate, DateTime? endDate)
        {
            var readings = await _readingRepository.GetByDeviceIdAsync(deviceId, startDate, endDate);
            return readings.Select(MapToSensorReadingDto).ToList();
        }

        // All readings for one sensor in optional time range (via repository).
        public async Task<List<SensorReadingDto>> GetReadingsBySensorIdAsync(int sensorId, DateTime? startDate, DateTime? endDate)
        {
            var readings = await _readingRepository.GetBySensorIdAsync(sensorId, startDate, endDate);
            return readings.Select(MapToSensorReadingDto).ToList();
        }

        // Bumps device LastSeenAt/UpdatedAt when the device row exists.
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
