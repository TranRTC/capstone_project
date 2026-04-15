using Microsoft.AspNetCore.SignalR;
using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Core.Interfaces;
using IoTMonitoringSystem.API.Hubs;

namespace IoTMonitoringSystem.API.Services
{
    // Pushes real-time events to browser clients via SignalR. Implements INotificationService so Services layer
    // does not reference SignalR types. Group names must match MonitoringHub (alerts, device_{id}, sensor_{id}, all_devices).
    public class SignalRNotificationService : INotificationService
    {
        private readonly IHubContext<MonitoringHub> _hubContext;

        public SignalRNotificationService(IHubContext<MonitoringHub> hubContext)
        {
            _hubContext = hubContext;
        }

        // New alert: subscribers on "alerts" and on the specific device group.
        public async Task NotifyNewAlertAsync(AlertDto alert)
        {
            await _hubContext.Clients.Group("alerts").SendAsync("NewAlert", alert);
            await _hubContext.Clients.Group($"device_{alert.DeviceId}").SendAsync("NewAlert", alert);
        }

        // Alert state changes: same routing as NewAlert (global "alerts" + device group).
        public async Task NotifyAlertAcknowledgedAsync(AlertDto alert)
        {
            await _hubContext.Clients.Group("alerts").SendAsync("AlertAcknowledged", alert);
            await _hubContext.Clients.Group($"device_{alert.DeviceId}").SendAsync("AlertAcknowledged", alert);
        }

        // Same groups as Acknowledged.
        public async Task NotifyAlertResolvedAsync(AlertDto alert)
        {
            await _hubContext.Clients.Group("alerts").SendAsync("AlertResolved", alert);
            await _hubContext.Clients.Group($"device_{alert.DeviceId}").SendAsync("AlertResolved", alert);
        }

        // Same groups as Acknowledged.
        public async Task NotifyAlertUpdatedAsync(AlertDto alert)
        {
            await _hubContext.Clients.Group("alerts").SendAsync("AlertUpdated", alert);
            await _hubContext.Clients.Group($"device_{alert.DeviceId}").SendAsync("AlertUpdated", alert);
        }

        // New reading: device detail, sensor detail, and dashboard-wide listeners.
        public async Task NotifySensorReadingAsync(SensorReadingDto reading)
        {
            await _hubContext.Clients.Group($"device_{reading.DeviceId}").SendAsync("SensorReadingReceived", reading);
            await _hubContext.Clients.Group($"sensor_{reading.SensorId}").SendAsync("SensorReadingReceived", reading);
            await _hubContext.Clients.Group("all_devices").SendAsync("SensorReadingReceived", reading);
        }

        // Online/offline-style updates for one device and for "all devices" dashboards.
        public async Task NotifyDeviceStatusChangedAsync(DeviceStatusDto status)
        {
            await _hubContext.Clients.Group($"device_{status.DeviceId}").SendAsync("DeviceStatusChanged", status);
            await _hubContext.Clients.Group("all_devices").SendAsync("DeviceStatusChanged", status);
        }
    }
}
