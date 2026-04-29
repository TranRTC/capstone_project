using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Infrastructure.Repositories;

namespace IoTMonitoringSystem.Services
{
    // Application service for devices: CRUD and last-seen. Maps DTOs to Device entities; uses IDeviceRepository for persistence.
    public class DeviceService : IDeviceService
    {
        private readonly IDeviceRepository _deviceRepository;

        public DeviceService(IDeviceRepository deviceRepository)
        {
            _deviceRepository = deviceRepository;
        }

        // Returns one device by id. Throws KeyNotFoundException if missing.
        public async Task<DeviceDto> GetDeviceByIdAsync(int deviceId)
        {
            var device = await _deviceRepository.GetByIdAsync(deviceId);
            if (device == null)
                throw new KeyNotFoundException($"Device with ID {deviceId} not found");

            return MapToDeviceDto(device);
        }

        // Returns all devices as list DTOs (fewer fields for grids/menus).
        public async Task<List<DeviceListDto>> GetAllDevicesAsync()
        {
            var devices = await _deviceRepository.GetAllAsync();
            return devices.Select(MapToDeviceListDto).ToList();
        }

        // Creates a device from input; sets IsActive and UTC CreatedAt/UpdatedAt.
        public async Task<DeviceDto> CreateDeviceAsync(CreateDeviceDto dto)
        {
            var device = new Device
            {
                DeviceName = dto.DeviceName,
                DeviceType = dto.DeviceType,
                Location = NormalizeOptional(dto.Location),
                FacilityType = NormalizeOptional(dto.FacilityType),
                EdgeDeviceType = NormalizeOptional(dto.EdgeDeviceType),
                EdgeDeviceId = NormalizeOptional(dto.EdgeDeviceId),
                Description = NormalizeOptional(dto.Description),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdDevice = await _deviceRepository.CreateAsync(device);
            return MapToDeviceDto(createdDevice);
        }

        // Partial update: only non-null DTO fields are applied. Throws KeyNotFoundException if missing.
        public async Task<DeviceDto> UpdateDeviceAsync(int deviceId, UpdateDeviceDto dto)
        {
            var device = await _deviceRepository.GetByIdAsync(deviceId);
            if (device == null)
                throw new KeyNotFoundException($"Device with ID {deviceId} not found");

            if (dto.DeviceName != null)
                device.DeviceName = dto.DeviceName;
            if (dto.DeviceType != null)
                device.DeviceType = dto.DeviceType;
            if (dto.Location != null)
                device.Location = NormalizeOptional(dto.Location);
            if (dto.FacilityType != null)
                device.FacilityType = NormalizeOptional(dto.FacilityType);
            if (dto.EdgeDeviceType != null)
                device.EdgeDeviceType = NormalizeOptional(dto.EdgeDeviceType);
            if (dto.EdgeDeviceId != null)
                device.EdgeDeviceId = NormalizeOptional(dto.EdgeDeviceId);
            if (dto.Description != null)
                device.Description = NormalizeOptional(dto.Description);
            if (dto.IsActive.HasValue)
                device.IsActive = dto.IsActive.Value;

            device.UpdatedAt = DateTime.UtcNow;

            var updatedDevice = await _deviceRepository.UpdateAsync(device);
            return MapToDeviceDto(updatedDevice);
        }

        // Deletes by id. Throws KeyNotFoundException if missing.
        public async Task DeleteDeviceAsync(int deviceId)
        {
            var device = await _deviceRepository.GetByIdAsync(deviceId);
            if (device == null)
                throw new KeyNotFoundException($"Device with ID {deviceId} not found");

            await _deviceRepository.DeleteAsync(device);
        }

        // Refreshes LastSeenAt/UpdatedAt only. status/message reserved for future DeviceStatusHistory. Throws if device missing.
        public async Task UpdateDeviceStatusAsync(int deviceId, string status, string? message = null)
        {
            var device = await _deviceRepository.GetByIdAsync(deviceId);
            if (device == null)
                throw new KeyNotFoundException($"Device with ID {deviceId} not found");

            device.LastSeenAt = DateTime.UtcNow;
            device.UpdatedAt = DateTime.UtcNow;
            await _deviceRepository.UpdateAsync(device);
        }

        // Updates LastSeenAt/UpdatedAt if the device exists; no-op if not found.
        public async Task UpdateDeviceLastSeenAsync(int deviceId)
        {
            var device = await _deviceRepository.GetByIdAsync(deviceId);
            if (device != null)
            {
                device.LastSeenAt = DateTime.UtcNow;
                device.UpdatedAt = DateTime.UtcNow;
                await _deviceRepository.UpdateAsync(device);
            }
        }

        private DeviceDto MapToDeviceDto(Device device)
        {
            return new DeviceDto
            {
                DeviceId = device.DeviceId,
                DeviceName = device.DeviceName,
                DeviceType = device.DeviceType,
                Location = device.Location,
                FacilityType = device.FacilityType,
                EdgeDeviceType = device.EdgeDeviceType,
                EdgeDeviceId = device.EdgeDeviceId,
                IsActive = device.IsActive,
                CreatedAt = device.CreatedAt,
                UpdatedAt = device.UpdatedAt,
                LastSeenAt = device.LastSeenAt,
                Description = device.Description
            };
        }

        private DeviceListDto MapToDeviceListDto(Device device)
        {
            return new DeviceListDto
            {
                DeviceId = device.DeviceId,
                DeviceName = device.DeviceName,
                DeviceType = device.DeviceType,
                Location = device.Location,
                IsActive = device.IsActive,
                LastSeenAt = device.LastSeenAt
            };
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
