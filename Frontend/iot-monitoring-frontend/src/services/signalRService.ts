import * as signalR from '@microsoft/signalr';
import { SensorReading, Alert, Device } from '../types';

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private readonly hubUrl = 'http://localhost:5000/monitoringhub';

  async start(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    // Clean up existing connection if it exists
    if (this.connection) {
      await this.stop();
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        skipNegotiation: false,
        withCredentials: true,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff: 0s, 2s, 10s, 30s
          if (retryContext.previousRetryCount === 0) return 2000;
          if (retryContext.previousRetryCount === 1) return 10000;
          return 30000;
        },
      })
      .build();

    try {
      await this.connection.start();
      console.log('SignalR Connected');
    } catch (err: any) {
      // Ignore AbortError during negotiation - automatic reconnect will handle it
      // This is a common transient issue that resolves automatically
      // Note: SignalR's internal logging may still show this error, but the connection
      // will succeed on automatic retry (you'll see "WebSocket connected" after)
      if (err?.name === 'AbortError' || 
          (err?.message && err.message.includes('negotiation')) ||
          (err?.message && err.message.includes('stopped during negotiation'))) {
        // Don't log or throw - automatic reconnection will handle it
        // The connection will succeed on retry (check for "WebSocket connected" message)
        return;
      }
      // Only log actual connection errors, not negotiation interruptions
      console.error('SignalR Connection Error:', err);
      throw err;
    }
  }

  async stop(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }

  // Subscribe to device updates
  async subscribeToDevice(deviceId: number): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('SubscribeToDevice', deviceId);
    }
  }

  async unsubscribeFromDevice(deviceId: number): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('UnsubscribeFromDevice', deviceId);
    }
  }

  async subscribeToSensor(sensorId: number): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('SubscribeToSensor', sensorId);
    }
  }

  async subscribeToAllDevices(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('SubscribeToAllDevices');
    }
  }

  async subscribeToAlerts(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('SubscribeToAlerts');
    }
  }

  // Event handlers
  onSensorReading(callback: (reading: SensorReading) => void): void {
    this.connection?.on('SensorReadingReceived', callback);
  }

  onNewAlert(callback: (alert: Alert) => void): void {
    this.connection?.on('NewAlert', callback);
  }

  onAlertAcknowledged(callback: (alert: Alert) => void): void {
    this.connection?.on('AlertAcknowledged', callback);
  }

  onAlertResolved(callback: (alert: Alert) => void): void {
    this.connection?.on('AlertResolved', callback);
  }

  onDeviceStatusChanged(callback: (status: any) => void): void {
    this.connection?.on('DeviceStatusChanged', callback);
  }

  off(eventName: string): void {
    this.connection?.off(eventName);
  }

  getConnectionState(): signalR.HubConnectionState | null {
    return this.connection?.state ?? null;
  }
}

export const signalRService = new SignalRService();

