using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using System.Buffers;
using System.Text;
using System.Text.Json;

namespace IoTMonitoringSystem.API.Services
{
    public class MqttService : IHostedService
    {
        private readonly ILogger<MqttService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private IMqttClient? _mqttClient;
        private MqttClientOptions? _mqttClientOptions;

        public MqttService(
            ILogger<MqttService> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var mqttHost = _configuration.GetValue<string>("Mqtt:Host", "localhost");
            var mqttPort = _configuration.GetValue<int>("Mqtt:Port", 1883);

            var factory = new MqttClientFactory();
            _mqttClient = factory.CreateMqttClient();

            _mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(mqttHost, mqttPort)
                .WithClientId("IoTMonitoringSystem_Server")
                .Build();

            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceived;

            try
            {
                await _mqttClient.ConnectAsync(_mqttClientOptions, cancellationToken);
                _logger.LogInformation($"Connected to MQTT broker at {mqttHost}:{mqttPort}");

                var subscribeOptions = factory.CreateSubscribeOptionsBuilder()
                    .WithTopicFilter(f => f.WithTopic("devices/+/sensors/+/readings"))
                    .WithTopicFilter(f => f.WithTopic("sensor/reading/+/+"))
                    .Build();

                await _mqttClient.SubscribeAsync(subscribeOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to MQTT.");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_mqttClient != null)
            {
                await _mqttClient.DisconnectAsync(cancellationToken: cancellationToken);
            }
        }

        private async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs args)
        {
            var topic = args.ApplicationMessage.Topic;

            // FIX: .ToArray() is needed because Payload is ReadOnlyMemory in v5.0
            var payload = args.ApplicationMessage.Payload.ToArray();

            _logger.LogInformation($"MQTT message received on topic: {topic}");

            await ProcessMessageAsync(topic, payload);
        }

        private async Task ProcessMessageAsync(string topic, byte[] payload)
        {
            try
            {
                var topicParts = topic.Split('/', StringSplitOptions.RemoveEmptyEntries);

                // Example: devices/1/sensors/2/readings
                if (topicParts.Length >= 4 && topicParts[0] == "devices" && topicParts[2] == "sensors")
                {
                    if (int.TryParse(topicParts[1], out int deviceId) && int.TryParse(topicParts[3], out int sensorId))
                    {
                        await ProcessSensorReading(deviceId, sensorId, payload);
                        return;
                    }
                }

                await ProcessJsonSensorReading(payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing topic: {topic}");
            }
        }

        private async Task ProcessSensorReading(int deviceId, int sensorId, byte[] payload)
        {
            try
            {
                var payloadString = Encoding.UTF8.GetString(payload);
                var jsonDocument = JsonDocument.Parse(payloadString);
                var root = jsonDocument.RootElement;

                var readingDto = new CreateSensorReadingDto
                {
                    DeviceId = deviceId,
                    SensorId = sensorId,
                    Value = root.GetProperty("value").GetDecimal(),
                    Timestamp = DateTime.UtcNow,
                    Status = "Normal",
                    Quality = "Good"
                };

                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ISensorReadingService>();
                await service.CreateReadingAsync(readingDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving sensor reading.");
            }
        }

        private async Task ProcessJsonSensorReading(byte[] payload)
        {
            try
            {
                var payloadString = Encoding.UTF8.GetString(payload);
                var jsonDocument = JsonDocument.Parse(payloadString);
                var root = jsonDocument.RootElement;

                if (root.TryGetProperty("deviceId", out var d) && root.TryGetProperty("value", out var v))
                {
                    var readingDto = new CreateSensorReadingDto
                    {
                        DeviceId = d.GetInt32(),
                        SensorId = root.TryGetProperty("sensorId", out var s) ? s.GetInt32() : 0,
                        Value = v.GetDecimal(),
                        Timestamp = DateTime.UtcNow
                    };

                    using var scope = _serviceProvider.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<ISensorReadingService>();
                    await service.CreateReadingAsync(readingDto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing JSON payload.");
            }
        }
    }
}