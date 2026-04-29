using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Infrastructure.Repositories;

namespace IoTMonitoringSystem.Services
{
    public class DeviceConfigurationService : IDeviceConfigurationService
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly IDeviceConfigurationRepository _configurationRepository;

        public DeviceConfigurationService(
            IDeviceRepository deviceRepository,
            IDeviceConfigurationRepository configurationRepository)
        {
            _deviceRepository = deviceRepository;
            _configurationRepository = configurationRepository;
        }

        public async Task<List<DeviceConfigurationDto>> GetByDeviceIdAsync(int deviceId)
        {
            var deviceExists = await _deviceRepository.ExistsAsync(deviceId);
            if (!deviceExists)
                throw new KeyNotFoundException($"Device with ID {deviceId} not found");

            var items = await _configurationRepository.GetByDeviceIdAsync(deviceId);
            return items.Select(MapToDto).ToList();
        }

        public async Task<List<DeviceConfigurationDto>> UpsertByDeviceIdAsync(int deviceId, UpsertDeviceConfigurationsDto dto)
        {
            var deviceExists = await _deviceRepository.ExistsAsync(deviceId);
            if (!deviceExists)
                throw new KeyNotFoundException($"Device with ID {deviceId} not found");

            if (dto.Configurations.Count == 0)
                return await GetByDeviceIdAsync(deviceId);

            foreach (var item in dto.Configurations)
            {
                if (string.IsNullOrWhiteSpace(item.ConfigurationKey))
                    continue;

                var key = item.ConfigurationKey.Trim();
                var existing = await _configurationRepository.GetByDeviceIdAndKeyAsync(deviceId, key);

                if (existing == null)
                {
                    var created = new DeviceConfiguration
                    {
                        DeviceId = deviceId,
                        ConfigurationKey = key,
                        ConfigurationValue = NormalizeOptional(item.ConfigurationValue),
                        ValueType = NormalizeOptional(item.ValueType),
                        Description = NormalizeOptional(item.Description),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _configurationRepository.CreateAsync(created);
                }
                else
                {
                    existing.ConfigurationValue = NormalizeOptional(item.ConfigurationValue);
                    existing.ValueType = NormalizeOptional(item.ValueType);
                    existing.Description = NormalizeOptional(item.Description);
                    existing.UpdatedAt = DateTime.UtcNow;
                    await _configurationRepository.UpdateAsync(existing);
                }
            }

            var updated = await _configurationRepository.GetByDeviceIdAsync(deviceId);
            return updated.Select(MapToDto).ToList();
        }

        private static DeviceConfigurationDto MapToDto(DeviceConfiguration item)
        {
            return new DeviceConfigurationDto
            {
                ConfigurationId = item.ConfigurationId,
                DeviceId = item.DeviceId,
                ConfigurationKey = item.ConfigurationKey,
                ConfigurationValue = item.ConfigurationValue,
                ValueType = item.ValueType,
                Description = item.Description,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            };
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
