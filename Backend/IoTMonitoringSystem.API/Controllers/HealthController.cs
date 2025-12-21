using Microsoft.AspNetCore.Mvc;
using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.API.Services;
using MQTTnet;
using System.Net.Sockets;

namespace IoTMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<HealthController> _logger;

        public HealthController(IConfiguration configuration, ILogger<HealthController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // GET: api/v1/health
        [HttpGet]
        public ActionResult<ApiResponse<object>> GetHealth()
        {
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "API is healthy",
                Data = new { status = "healthy", timestamp = DateTime.UtcNow }
            });
        }

        // GET: api/v1/health/mqtt
        [HttpGet("mqtt")]
        public async Task<ActionResult<ApiResponse<object>>> GetMqttBrokerStatus()
        {
            var mqttHost = _configuration.GetValue<string>("Mqtt:Host", "localhost");
            var mqttPort = _configuration.GetValue<int>("Mqtt:Port", 1883);

            try
            {
                // First, check if the port is accessible (TCP connection test)
                bool isPortAccessible = await CheckTcpConnectionAsync(mqttHost, mqttPort);

                if (!isPortAccessible)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "MQTT broker is not accessible",
                        Data = new
                        {
                            status = "unavailable",
                            host = mqttHost,
                            port = mqttPort,
                            accessible = false,
                            timestamp = DateTime.UtcNow
                        }
                    });
                }

                // Try to connect with MQTT client to verify MQTT protocol
                bool isMqttReady = await CheckMqttConnectionAsync(mqttHost, mqttPort);

                return Ok(new ApiResponse<object>
                {
                    Success = isMqttReady,
                    Message = isMqttReady ? "MQTT broker is ready" : "MQTT broker port is open but MQTT connection failed",
                    Data = new
                    {
                        status = isMqttReady ? "ready" : "port_open_but_mqtt_failed",
                        host = mqttHost,
                        port = mqttPort,
                        accessible = isPortAccessible,
                        mqttReady = isMqttReady,
                        timestamp = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking MQTT broker status");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error checking MQTT broker status",
                    Errors = new List<string> { ex.Message },
                    Data = new
                    {
                        status = "error",
                        host = mqttHost,
                        port = mqttPort,
                        timestamp = DateTime.UtcNow
                    }
                });
            }
        }

        private async Task<bool> CheckTcpConnectionAsync(string host, int port)
        {
            try
            {
                using var tcpClient = new TcpClient();
                var connectTask = tcpClient.ConnectAsync(host, port);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(2));

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    return false; // Timeout
                }

                return tcpClient.Connected;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> CheckMqttConnectionAsync(string host, int port)
        {
            try
            {
                var factory = new MqttClientFactory();
                using var mqttClient = factory.CreateMqttClient();

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(host, port)
                    .WithClientId($"HealthCheck_{Guid.NewGuid()}")
                    .WithTimeout(TimeSpan.FromSeconds(2))
                    .Build();

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await mqttClient.ConnectAsync(options, cts.Token);

                if (mqttClient.IsConnected)
                {
                    await mqttClient.DisconnectAsync(cancellationToken: cts.Token);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}

