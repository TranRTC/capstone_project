import axios, { AxiosInstance } from 'axios';
import {
  Device,
  DeviceList,
  CreateDevice,
  Sensor,
  CreateSensor,
  SensorReading,
  CreateSensorReading,
  Alert,
  AlertRule,
  CreateAlertRule,
  ApiResponse,
  PagedResult,
} from '../types';

const API_BASE_URL = 'http://localhost:5286/api/v1';

class ApiService {
  private api: AxiosInstance;

  constructor() {
    this.api = axios.create({
      baseURL: API_BASE_URL,
      headers: {
        'Content-Type': 'application/json',
      },
    });
  }

  // Device endpoints
  async getDevices(): Promise<DeviceList[]> {
    const response = await this.api.get<ApiResponse<DeviceList[]>>('/devices');
    return response.data.data || [];
  }

  async getDevice(id: number): Promise<Device> {
    const response = await this.api.get<ApiResponse<Device>>(`/devices/${id}`);
    if (!response.data.data) {
      throw new Error('Device not found');
    }
    return response.data.data;
  }

  async createDevice(device: CreateDevice): Promise<Device> {
    const response = await this.api.post<ApiResponse<Device>>('/devices', device);
    if (!response.data.data) {
      throw new Error('Failed to create device');
    }
    return response.data.data;
  }

  async updateDevice(id: number, device: Partial<CreateDevice>): Promise<Device> {
    const response = await this.api.put<ApiResponse<Device>>(`/devices/${id}`, device);
    if (!response.data.data) {
      throw new Error('Failed to update device');
    }
    return response.data.data;
  }

  async deleteDevice(id: number): Promise<void> {
    await this.api.delete(`/devices/${id}`);
  }

  // Sensor endpoints
  async getSensorsByDevice(deviceId: number): Promise<Sensor[]> {
    const response = await this.api.get<ApiResponse<Sensor[]>>(`/sensors/devices/${deviceId}/sensors`);
    return response.data.data || [];
  }

  async getSensor(id: number): Promise<Sensor> {
    const response = await this.api.get<ApiResponse<Sensor>>(`/sensors/${id}`);
    if (!response.data.data) {
      throw new Error('Sensor not found');
    }
    return response.data.data;
  }

  async createSensor(deviceId: number, sensor: CreateSensor): Promise<Sensor> {
    const response = await this.api.post<ApiResponse<Sensor>>(
      `/sensors/devices/${deviceId}/sensors`,
      sensor
    );
    if (!response.data.data) {
      throw new Error('Failed to create sensor');
    }
    return response.data.data;
  }

  async updateSensor(id: number, sensor: Partial<CreateSensor>): Promise<Sensor> {
    const response = await this.api.put<ApiResponse<Sensor>>(`/sensors/${id}`, sensor);
    if (!response.data.data) {
      throw new Error('Failed to update sensor');
    }
    return response.data.data;
  }

  async deleteSensor(id: number): Promise<void> {
    await this.api.delete(`/sensors/${id}`);
  }

  // Sensor Reading endpoints
  async createSensorReading(reading: CreateSensorReading): Promise<SensorReading> {
    const response = await this.api.post<ApiResponse<SensorReading>>('/sensorreadings', reading);
    if (!response.data.data) {
      throw new Error('Failed to create sensor reading');
    }
    return response.data.data;
  }

  async getSensorReadings(params: {
    deviceId?: number;
    sensorId?: number;
    startDate?: string;
    endDate?: string;
    pageNumber?: number;
    pageSize?: number;
  }): Promise<PagedResult<SensorReading>> {
    const response = await this.api.get<ApiResponse<PagedResult<SensorReading>>>(
      '/sensorreadings',
      { params }
    );
    if (!response.data.data) {
      throw new Error('Failed to get sensor readings');
    }
    return response.data.data;
  }

  // Alert endpoints
  async getActiveAlerts(): Promise<Alert[]> {
    const response = await this.api.get<ApiResponse<Alert[]>>('/alerts/active');
    return response.data.data || [];
  }

  async getAlerts(params: {
    status?: string;
    severity?: string;
    deviceId?: number;
    pageNumber?: number;
    pageSize?: number;
  }): Promise<PagedResult<Alert>> {
    const response = await this.api.get<ApiResponse<PagedResult<Alert>>>('/alerts', { params });
    if (!response.data.data) {
      throw new Error('Failed to get alerts');
    }
    return response.data.data;
  }

  async acknowledgeAlert(id: number): Promise<Alert> {
    const response = await this.api.put<ApiResponse<Alert>>(`/alerts/${id}/acknowledge`);
    if (!response.data.data) {
      throw new Error('Failed to acknowledge alert');
    }
    return response.data.data;
  }

  async resolveAlert(id: number): Promise<Alert> {
    const response = await this.api.put<ApiResponse<Alert>>(`/alerts/${id}/resolve`);
    if (!response.data.data) {
      throw new Error('Failed to resolve alert');
    }
    return response.data.data;
  }

  // Alert Rule endpoints
  async getAlertRules(): Promise<AlertRule[]> {
    const response = await this.api.get<ApiResponse<AlertRule[]>>('/alertrules');
    return response.data.data || [];
  }

  async createAlertRule(rule: CreateAlertRule): Promise<AlertRule> {
    const response = await this.api.post<ApiResponse<AlertRule>>('/alertrules', rule);
    if (!response.data.data) {
      throw new Error('Failed to create alert rule');
    }
    return response.data.data;
  }

  async updateAlertRule(id: number, rule: Partial<CreateAlertRule>): Promise<AlertRule> {
    const response = await this.api.put<ApiResponse<AlertRule>>(`/alertrules/${id}`, rule);
    if (!response.data.data) {
      throw new Error('Failed to update alert rule');
    }
    return response.data.data;
  }

  async deleteAlertRule(id: number): Promise<void> {
    await this.api.delete(`/alertrules/${id}`);
  }
}

export const apiService = new ApiService();

