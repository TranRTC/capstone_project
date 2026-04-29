using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Infrastructure.Data;
using IoTMonitoringSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;

namespace IoTMonitoringSystem.Services
{
    public class DeviceCommandService : IDeviceCommandService
    {
        private readonly IDeviceCommandRepository _deviceCommandRepository;
        private readonly IDeviceRepository _deviceRepository;
        private readonly ApplicationDbContext _context;
        private readonly IDeviceCommandDispatcher? _commandDispatcher;

        public DeviceCommandService(
            IDeviceCommandRepository deviceCommandRepository,
            IDeviceRepository deviceRepository,
            ApplicationDbContext context,
            IDeviceCommandDispatcher? commandDispatcher = null)
        {
            _deviceCommandRepository = deviceCommandRepository;
            _deviceRepository = deviceRepository;
            _context = context;
            _commandDispatcher = commandDispatcher;
        }

        public async Task<DeviceCommandDto> CreateCommandAsync(int deviceId, CreateDeviceCommandDto dto, string? requestedBy = null)
        {
            var device = await _deviceRepository.GetByIdAsync(deviceId);
            if (device == null)
                throw new KeyNotFoundException($"Device with ID {deviceId} not found");

            if (!device.IsActive)
                throw new InvalidOperationException($"Device with ID {deviceId} is inactive");

            await ValidateCommandAsync(device, dto);

            var command = new DeviceCommand
            {
                DeviceId = deviceId,
                CommandType = dto.CommandType.Trim(),
                Payload = dto.Payload,
                Status = "Pending",
                CorrelationId = dto.CorrelationId,
                RequestedBy = requestedBy,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _deviceCommandRepository.CreateAsync(command);

            if (_commandDispatcher != null)
            {
                try
                {
                    await _commandDispatcher.DispatchAsync(created);
                    created.Status = "Sent";
                    created.SentAt = DateTime.UtcNow;
                    created.ErrorMessage = null;
                }
                catch (Exception ex)
                {
                    created.Status = "Failed";
                    created.CompletedAt = DateTime.UtcNow;
                    created.ErrorMessage = ex.Message;
                }

                created = await _deviceCommandRepository.UpdateAsync(created);
            }

            return MapToDto(created);
        }

        public async Task<PagedResult<DeviceCommandDto>> GetCommandsByDeviceIdAsync(int deviceId, DeviceCommandQueryDto query)
        {
            var device = await _deviceRepository.GetByIdAsync(deviceId);
            if (device == null)
                throw new KeyNotFoundException($"Device with ID {deviceId} not found");

            var dbQuery = _context.DeviceCommands
                .Where(c => c.DeviceId == deviceId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Status))
                dbQuery = dbQuery.Where(c => c.Status == query.Status);

            if (query.StartDate.HasValue)
                dbQuery = dbQuery.Where(c => c.CreatedAt >= query.StartDate.Value);

            if (query.EndDate.HasValue)
                dbQuery = dbQuery.Where(c => c.CreatedAt <= query.EndDate.Value);

            var totalCount = await dbQuery.CountAsync();

            var items = await dbQuery
                .OrderByDescending(c => c.CreatedAt)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<DeviceCommandDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }

        public async Task<DeviceCommandDto> GetCommandByIdAsync(long commandId)
        {
            var command = await _context.DeviceCommands
                .FirstOrDefaultAsync(c => c.CommandId == commandId);

            if (command == null)
                throw new KeyNotFoundException($"Command with ID {commandId} not found");

            return MapToDto(command);
        }

        public async Task<DeviceCommandDto> UpdateCommandStatusAsync(long commandId, string status, string? errorMessage = null)
        {
            var command = await _context.DeviceCommands
                .FirstOrDefaultAsync(c => c.CommandId == commandId);

            if (command == null)
                throw new KeyNotFoundException($"Command with ID {commandId} not found");

            command.Status = status;

            if (status == "Sent" && !command.SentAt.HasValue)
                command.SentAt = DateTime.UtcNow;

            if (status is "Acked" or "Failed" or "Timeout")
                command.CompletedAt = DateTime.UtcNow;

            command.ErrorMessage = errorMessage;

            var updated = await _deviceCommandRepository.UpdateAsync(command);
            return MapToDto(updated);
        }

        private async Task ValidateCommandAsync(Device device, CreateDeviceCommandDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CommandType))
                throw new ArgumentException("CommandType is required");

            if (string.IsNullOrWhiteSpace(dto.Payload))
                throw new ArgumentException("Payload is required");

            JsonElement payload;
            try
            {
                payload = JsonDocument.Parse(dto.Payload).RootElement;
            }
            catch (JsonException)
            {
                throw new ArgumentException("Payload must be valid JSON");
            }

            var commandType = dto.CommandType.Trim();
            var supportsPowerControl = await GetSupportsFlagAsync(device.DeviceId, "supportsPowerControl");
            var supportsAnalogControl = await GetSupportsFlagAsync(device.DeviceId, "supportsAnalogControl");
            var isActuatorType = IsActuatorType(device.DeviceType);

            if (commandType.Equals("SetPower", StringComparison.OrdinalIgnoreCase))
            {
                if (!supportsPowerControl && !isActuatorType)
                    throw new InvalidOperationException("Device does not support power control");

                if (!payload.TryGetProperty("on", out var onProp) ||
                    (onProp.ValueKind != JsonValueKind.True && onProp.ValueKind != JsonValueKind.False))
                {
                    throw new ArgumentException("SetPower payload must include boolean property 'on'");
                }

                return;
            }

            if (commandType.Equals("SetValue", StringComparison.OrdinalIgnoreCase))
            {
                if (!supportsAnalogControl && !isActuatorType)
                    throw new InvalidOperationException("Device does not support analog control");

                if (!payload.TryGetProperty("value", out var valueProp) || valueProp.ValueKind != JsonValueKind.Number)
                    throw new ArgumentException("SetValue payload must include numeric property 'value'");

                var value = valueProp.GetDecimal();
                var min = await GetDecimalConfigAsync(device.DeviceId, "analogMin");
                var max = await GetDecimalConfigAsync(device.DeviceId, "analogMax");

                if (min.HasValue && value < min.Value)
                    throw new ArgumentOutOfRangeException(nameof(dto.Payload), $"Value is below analogMin ({min.Value})");

                if (max.HasValue && value > max.Value)
                    throw new ArgumentOutOfRangeException(nameof(dto.Payload), $"Value is above analogMax ({max.Value})");

                return;
            }

            throw new ArgumentException($"Unsupported command type '{commandType}'");
        }

        private static bool IsActuatorType(string? deviceType)
        {
            if (string.IsNullOrWhiteSpace(deviceType))
                return false;

            return deviceType.Contains("motor", StringComparison.OrdinalIgnoreCase)
                   || deviceType.Contains("actuator", StringComparison.OrdinalIgnoreCase)
                   || deviceType.Contains("controller", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<bool> GetSupportsFlagAsync(int deviceId, string key)
        {
            var config = await _context.DeviceConfigurations
                .Where(c => c.DeviceId == deviceId && c.ConfigurationKey == key)
                .OrderByDescending(c => c.UpdatedAt)
                .Select(c => c.ConfigurationValue)
                .FirstOrDefaultAsync();

            return bool.TryParse(config, out var value) && value;
        }

        private async Task<decimal?> GetDecimalConfigAsync(int deviceId, string key)
        {
            var config = await _context.DeviceConfigurations
                .Where(c => c.DeviceId == deviceId && c.ConfigurationKey == key)
                .OrderByDescending(c => c.UpdatedAt)
                .Select(c => c.ConfigurationValue)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(config))
                return null;

            return decimal.TryParse(config, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
                ? value
                : null;
        }

        private static DeviceCommandDto MapToDto(DeviceCommand command)
        {
            return new DeviceCommandDto
            {
                CommandId = command.CommandId,
                DeviceId = command.DeviceId,
                CommandType = command.CommandType,
                Payload = command.Payload,
                Status = command.Status,
                CorrelationId = command.CorrelationId,
                CreatedAt = command.CreatedAt,
                SentAt = command.SentAt,
                CompletedAt = command.CompletedAt,
                ErrorMessage = command.ErrorMessage,
                RequestedBy = command.RequestedBy
            };
        }
    }
}
