using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Infrastructure.Data;
using IoTMonitoringSystem.Services;
using Microsoft.EntityFrameworkCore;
using MQTTnet;
using System.Text;
using System.Text.Json;

namespace IoTMonitoringSystem.API.Services
{
    public class MqttCommandDispatcher : IDeviceCommandDispatcher
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MqttCommandDispatcher> _logger;
        private readonly ApplicationDbContext _context;

        public MqttCommandDispatcher(
            IConfiguration configuration,
            ILogger<MqttCommandDispatcher> logger,
            ApplicationDbContext context)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
        }

        public async Task DispatchAsync(DeviceCommand command, CancellationToken cancellationToken = default)
        {
            var mqttHost = _configuration.GetValue<string>("Mqtt:Host", "localhost");
            var mqttPort = _configuration.GetValue<int>("Mqtt:Port", 1883);

            var factory = new MqttClientFactory();
            using var mqttClient = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(mqttHost, mqttPort)
                .WithClientId($"IoTMonitoringSystem_CommandDispatcher_{Guid.NewGuid():N}")
                .Build();

            await mqttClient.ConnectAsync(options, cancellationToken);

            var topic = $"devices/{command.DeviceId}/commands";
            var payload = await BuildCommandPayloadAsync(command, cancellationToken);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await mqttClient.PublishAsync(message, cancellationToken);
            _logger.LogInformation("Published command {CommandId} to topic {Topic}", command.CommandId, topic);

            await mqttClient.DisconnectAsync(cancellationToken: cancellationToken);
        }

        private async Task<byte[]> BuildCommandPayloadAsync(DeviceCommand command, CancellationToken cancellationToken)
        {
            var payloadElement = JsonSerializer.Deserialize<JsonElement>(command.Payload);
            string? channel = null;
            if (command.ActuatorId.HasValue)
            {
                var actuator = await _context.Actuators.AsNoTracking()
                    .FirstOrDefaultAsync(a => a.ActuatorId == command.ActuatorId.Value, cancellationToken);
                channel = actuator?.Channel;
            }

            var commandEnvelope = new
            {
                commandId = command.CommandId,
                correlationId = command.CorrelationId,
                commandType = command.CommandType,
                payload = payloadElement,
                createdAt = command.CreatedAt,
                actuatorId = command.ActuatorId,
                channel
            };

            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(commandEnvelope));
        }
    }
}
