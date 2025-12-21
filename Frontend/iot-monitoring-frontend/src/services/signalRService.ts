import * as signalR from '@microsoft/signalr';
import { SensorReading, Alert, Device } from '../types';

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private readonly hubUrl = 'http://localhost:5286/monitoringhub';

  async start(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl)
      .withAutomaticReconnect()
      .build();

    try {
      await this.connection.start();
      console.log('SignalR Connected');
    } catch (err) {
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

