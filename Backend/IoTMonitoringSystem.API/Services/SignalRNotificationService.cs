using Microsoft.AspNetCore.SignalR;
using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Core.Interfaces;
using IoTMonitoringSystem.API.Hubs;

namespace IoTMonitoringSystem.API.Services
{
    public class SignalRNotificationService : INotificationService
    {
        private readonly IHubContext<MonitoringHub> _hubContext;

        public SignalRNotificationService(IHubContext<MonitoringHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyNewAlertAsync(AlertDto alert)
        {
            await _hubContext.Clients.Group("alerts").SendAsync("NewAlert", alert);
            await _hubContext.Clients.Group($"device_{alert.DeviceId}").SendAsync("NewAlert", alert);
        }

        public async Task NotifyAlertAcknowledgedAsync(AlertDto alert)
        {
            await _hubContext.Clients.Group("alerts").SendAsync("AlertAcknowledged", alert);
            await _hubContext.Clients.Group($"device_{alert.DeviceId}").SendAsync("AlertAcknowledged", alert);
        }

        public async Task NotifyAlertResolvedAsync(AlertDto alert)
        {
            await _hubContext.Clients.Group("alerts").SendAsync("AlertResolved", alert);
            await _hubContext.Clients.Group($"device_{alert.DeviceId}").SendAsync("AlertResolved", alert);
        }

        public async Task NotifyAlertUpdatedAsync(AlertDto alert)
        {
            await _hubContext.Clients.Group("alerts").SendAsync("AlertUpdated", alert);
            await _hubContext.Clients.Group($"device_{alert.DeviceId}").SendAsync("AlertUpdated", alert);
        }

        public async Task NotifySensorReadingAsync(SensorReadingDto reading)
        {
            await _hubContext.Clients.Group($"device_{reading.DeviceId}").SendAsync("SensorReadingReceived", reading);
            await _hubContext.Clients.Group($"sensor_{reading.SensorId}").SendAsync("SensorReadingReceived", reading);
            await _hubContext.Clients.Group("all_devices").SendAsync("SensorReadingReceived", reading);
        }

        public async Task NotifyDeviceStatusChangedAsync(DeviceStatusDto status)
        {
            await _hubContext.Clients.Group($"device_{status.DeviceId}").SendAsync("DeviceStatusChanged", status);
            await _hubContext.Clients.Group("all_devices").SendAsync("DeviceStatusChanged", status);
        }
    }
}

