using Microsoft.AspNetCore.SignalR;

namespace IoTMonitoringSystem.API.Hubs
{
    public class MonitoringHub : Hub
    {
        // Client-to-Server Methods
        public async Task SubscribeToDevice(int deviceId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"device_{deviceId}");
            await Clients.Caller.SendAsync("Subscribed", $"Subscribed to device {deviceId}");
        }

        public async Task UnsubscribeFromDevice(int deviceId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"device_{deviceId}");
            await Clients.Caller.SendAsync("Unsubscribed", $"Unsubscribed from device {deviceId}");
        }

        public async Task SubscribeToSensor(int sensorId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"sensor_{sensorId}");
            await Clients.Caller.SendAsync("Subscribed", $"Subscribed to sensor {sensorId}");
        }

        public async Task UnsubscribeFromSensor(int sensorId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sensor_{sensorId}");
            await Clients.Caller.SendAsync("Unsubscribed", $"Unsubscribed from sensor {sensorId}");
        }

        public async Task SubscribeToAllDevices()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "all_devices");
            await Clients.Caller.SendAsync("Subscribed", "Subscribed to all devices");
        }

        public async Task SubscribeToAlerts()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "alerts");
            await Clients.Caller.SendAsync("Subscribed", "Subscribed to alerts");
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            await Clients.Caller.SendAsync("Connected", "Connected to monitoring hub");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}

