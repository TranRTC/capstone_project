using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Infrastructure.Data;
using IoTMonitoringSystem.Services;
using Microsoft.EntityFrameworkCore;
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
    public class MqttService : BackgroundService, IMqttRuntimeState
    {
        private readonly ILogger<MqttService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private IMqttClient? _mqttClient;
        private MqttClientOptions? _mqttClientOptions;
        private readonly object _stateLock = new();
        private bool _isConnected;
        private bool _isSubscribed;
        private DateTime? _lastConnectAttemptAtUtc;
        private DateTime? _lastConnectedAtUtc;
        private DateTime? _lastMessageReceivedAtUtc;
        private int _reconnectAttempts;
        private string? _lastError;

        public bool IsConnected { get { lock (_stateLock) return _isConnected; } }
        public bool IsSubscribed { get { lock (_stateLock) return _isSubscribed; } }
        public DateTime? LastConnectAttemptAtUtc { get { lock (_stateLock) return _lastConnectAttemptAtUtc; } }
        public DateTime? LastConnectedAtUtc { get { lock (_stateLock) return _lastConnectedAtUtc; } }
        public DateTime? LastMessageReceivedAtUtc { get { lock (_stateLock) return _lastMessageReceivedAtUtc; } }
        public int ReconnectAttempts { get { lock (_stateLock) return _reconnectAttempts; } }
        public string? LastError { get { lock (_stateLock) return _lastError; } }

        public MqttService(
            ILogger<MqttService> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var mqttHost = _configuration.GetValue<string>("Mqtt:Host", "localhost");
            var mqttPort = _configuration.GetValue<int>("Mqtt:Port", 1883);

            var factory = new MqttClientFactory();
            _mqttClient = factory.CreateMqttClient();

            _mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(mqttHost, mqttPort)
                .WithClientId("IoTMonitoringSystem_Server")
                .Build();

            _mqttClient.ConnectedAsync += args =>
            {
                lock (_stateLock)
                {
                    _isConnected = true;
                    _lastConnectedAtUtc = DateTime.UtcNow;
                    _lastError = null;
                    _reconnectAttempts = 0;
                }
                _logger.LogInformation("Connected to MQTT broker at {Host}:{Port}", mqttHost, mqttPort);
                return Task.CompletedTask;
            };
            _mqttClient.DisconnectedAsync += args =>
            {
                lock (_stateLock)
                {
                    _isConnected = false;
                    _isSubscribed = false;
                    _lastError = args.Exception?.Message ?? _lastError;
                }
                _logger.LogWarning(args.Exception, "MQTT disconnected. Will retry connection.");
                return Task.CompletedTask;
            };
            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceived;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_mqttClient.IsConnected)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                        continue;
                    }

                    lock (_stateLock)
                    {
                        _lastConnectAttemptAtUtc = DateTime.UtcNow;
                        _reconnectAttempts += 1;
                    }

                    await _mqttClient.ConnectAsync(_mqttClientOptions, stoppingToken);

                    var subscribeOptions = factory.CreateSubscribeOptionsBuilder()
                        .WithTopicFilter(f => f.WithTopic("devices/+/sensors/+/readings"))
                        .WithTopicFilter(f => f.WithTopic("sensor/reading/+/+"))
                        .WithTopicFilter(f => f.WithTopic("devices/+/commands/ack"))
                        .Build();

                    await _mqttClient.SubscribeAsync(subscribeOptions, stoppingToken);
                    lock (_stateLock)
                    {
                        _isSubscribed = true;
                    }
                    _logger.LogInformation("Subscribed to MQTT topics for readings and command acknowledgements.");
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    lock (_stateLock)
                    {
                        _isConnected = false;
                        _isSubscribed = false;
                        _lastError = ex.Message;
                    }
                    _logger.LogError(ex, "Failed to connect/subscribe to MQTT. Retrying in 5 seconds.");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_mqttClient != null)
            {
                if (_mqttClient.IsConnected)
                {
                    await _mqttClient.DisconnectAsync(cancellationToken: cancellationToken);
                }
                _mqttClient.Dispose();
            }
            await base.StopAsync(cancellationToken);
        }

        private async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs args)
        {
            var topic = args.ApplicationMessage.Topic;

            // FIX: .ToArray() is needed because Payload is ReadOnlyMemory in v5.0
            var payload = args.ApplicationMessage.Payload.ToArray();
            lock (_stateLock)
            {
                _lastMessageReceivedAtUtc = DateTime.UtcNow;
                _lastError = null;
            }

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

                if (topicParts.Length >= 4 && topicParts[0] == "devices" && topicParts[2] == "commands" && topicParts[3] == "ack")
                {
                    if (int.TryParse(topicParts[1], out int ackDeviceId))
                    {
                        await ProcessCommandAck(ackDeviceId, payload);
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

        private async Task ProcessCommandAck(int deviceId, byte[] payload)
        {
            try
            {
                var payloadString = Encoding.UTF8.GetString(payload);
                var jsonDocument = JsonDocument.Parse(payloadString);
                var root = jsonDocument.RootElement;

                if (!root.TryGetProperty("commandId", out var commandIdProperty))
                {
                    _logger.LogWarning("Command ACK missing commandId for device {DeviceId}", deviceId);
                    return;
                }

                var commandId = commandIdProperty.GetInt64();
                var status = root.TryGetProperty("status", out var statusProperty)
                    ? statusProperty.GetString() ?? "Acked"
                    : "Acked";
                var errorMessage = root.TryGetProperty("errorMessage", out var errorProperty)
                    ? errorProperty.GetString()
                    : null;

                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var existing = await db.DeviceCommands.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CommandId == commandId);

                if (existing == null)
                {
                    _logger.LogWarning("Command ACK references unknown commandId {CommandId}", commandId);
                    return;
                }

                if (existing.DeviceId != deviceId)
                {
                    _logger.LogWarning(
                        "Command ACK device mismatch for command {CommandId}: topic device {TopicDeviceId}, command device {CommandDeviceId}",
                        commandId,
                        deviceId,
                        existing.DeviceId);
                    return;
                }

                var commandService = scope.ServiceProvider.GetRequiredService<IDeviceCommandService>();
                await commandService.UpdateCommandStatusAsync(commandId, status, errorMessage);

                _logger.LogInformation(
                    "Processed command ACK for command {CommandId}, device {DeviceId}, status {Status}",
                    commandId,
                    deviceId,
                    status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing command ACK for device {DeviceId}.", deviceId);
            }
        }
    }
}