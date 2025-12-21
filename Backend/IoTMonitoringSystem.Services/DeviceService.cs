using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Infrastructure.Repositories;

namespace IoTMonitoringSystem.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly IDeviceRepository _deviceRepository;

        public DeviceService(IDeviceRepository deviceRepository)
        {
            _deviceRepository = deviceRepository;
        }

        public async Task<DeviceDto> GetDeviceByIdAsync(int deviceId)
        {
            var device = await _deviceRepository.GetByIdAsync(deviceId);
            if (device == null)
                throw new KeyNotFoundException($"Device with ID {deviceId} not found");

            return MapToDeviceDto(device);
        }

        public async Task<List<DeviceListDto>> GetAllDevicesAsync()
        {
            var devices = await _deviceRepository.GetAllAsync();
            return devices.Select(MapToDeviceListDto).ToList();
        }

        public async Task<DeviceDto> CreateDeviceAsync(CreateDeviceDto dto)
        {
            var device = new Device
            {
                DeviceName = dto.DeviceName,
                DeviceType = dto.DeviceType,
                Location = dto.Location,
                FacilityType = dto.FacilityType,
                EdgeDeviceType = dto.EdgeDeviceType,
                EdgeDeviceId = dto.EdgeDeviceId,
                Description = dto.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdDevice = await _deviceRepository.CreateAsync(device);
            return MapToDeviceDto(createdDevice);
        }

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
                device.Location = dto.Location;
            if (dto.FacilityType != null)
                device.FacilityType = dto.FacilityType;
            if (dto.Description != null)
                device.Description = dto.Description;
            if (dto.IsActive.HasValue)
                device.IsActive = dto.IsActive.Value;

            device.UpdatedAt = DateTime.UtcNow;

            var updatedDevice = await _deviceRepository.UpdateAsync(device);
            return MapToDeviceDto(updatedDevice);
        }

        public async Task DeleteDeviceAsync(int deviceId)
        {
            var device = await _deviceRepository.GetByIdAsync(deviceId);
            if (device == null)
                throw new KeyNotFoundException($"Device with ID {deviceId} not found");

            await _deviceRepository.DeleteAsync(device);
        }

        public async Task UpdateDeviceStatusAsync(int deviceId, string status, string? message = null)
        {
            var device = await _deviceRepository.GetByIdAsync(deviceId);
            if (device == null)
                throw new KeyNotFoundException($"Device with ID {deviceId} not found");

            // This will be implemented when we add DeviceStatusHistory service
            // For now, just update LastSeenAt
            device.LastSeenAt = DateTime.UtcNow;
            device.UpdatedAt = DateTime.UtcNow;
            await _deviceRepository.UpdateAsync(device);
        }

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
    }
}

