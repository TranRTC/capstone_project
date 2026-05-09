using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Infrastructure.Data;
using IoTMonitoringSystem.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace IoTMonitoringSystem.API.Services
{
    /// <summary>
    /// Dispatches device commands via the persistent MqttService connection
    /// instead of opening a new TCP connection per command.
    /// </summary>
    public class MqttCommandDispatcher : IDeviceCommandDispatcher
    {
        private readonly IMqttPublisher _mqttPublisher;
        private readonly ILogger<MqttCommandDispatcher> _logger;
        private readonly ApplicationDbContext _context;

        public MqttCommandDispatcher(
            IMqttPublisher mqttPublisher,
            ILogger<MqttCommandDispatcher> logger,
            ApplicationDbContext context)
        {
            _mqttPublisher = mqttPublisher;
            _logger = logger;
            _context = context;
        }

        public async Task DispatchAsync(DeviceCommand command, CancellationToken cancellationToken = default)
        {
            if (!_mqttPublisher.IsConnected)
            {
                throw new InvalidOperationException(
                    "MQTT broker is not connected. Command cannot be dispatched until the connection is restored.");
            }

            var topic = $"devices/{command.DeviceId}/commands";
            var payload = await BuildCommandPayloadAsync(command, cancellationToken);

            await _mqttPublisher.PublishAsync(topic, payload, cancellationToken);

            _logger.LogInformation(
                "Dispatched command {CommandId} (type={CommandType}) to topic {Topic}",
                command.CommandId,
                command.CommandType,
                topic);
        }

        private async Task<string> BuildCommandPayloadAsync(DeviceCommand command, CancellationToken cancellationToken)
        {
            var payloadElement = JsonSerializer.Deserialize<JsonElement>(command.Payload);
            string? channel = null;

            if (command.ActuatorId.HasValue)
            {
                var actuator = await _context.Actuators.AsNoTracking()
                    .FirstOrDefaultAsync(a => a.ActuatorId == command.ActuatorId.Value, cancellationToken);
                channel = actuator?.Channel;
            }

            var envelope = new
            {
                commandId = command.CommandId,
                correlationId = command.CorrelationId,
                commandType = command.CommandType,
                payload = payloadElement,
                createdAt = command.CreatedAt,
                actuatorId = command.ActuatorId,
                channel
            };

            return JsonSerializer.Serialize(envelope);
        }
    }
}
