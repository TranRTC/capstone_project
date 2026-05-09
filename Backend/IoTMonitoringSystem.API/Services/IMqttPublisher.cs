namespace IoTMonitoringSystem.API.Services
{
    /// <summary>
    /// Exposes publish capability of the persistent MqttService connection
    /// so that other services can send messages without opening a new connection.
    /// </summary>
    public interface IMqttPublisher
    {
        /// <summary>Returns true if the persistent MQTT connection is currently active.</summary>
        bool IsConnected { get; }

        /// <summary>Publishes a UTF-8 string payload to the given topic using QoS 1.</summary>
        Task PublishAsync(string topic, string payload, CancellationToken cancellationToken = default);
    }
}
