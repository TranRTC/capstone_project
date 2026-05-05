import axios, { AxiosInstance } from 'axios';
import {
  Device,
  DeviceList,
  CreateDevice,
  Sensor,
  CreateSensor,
  UpdateSensorPayload,
  SensorReading,
  CreateSensorReading,
  DeviceCommand,
  CreateDeviceCommand,
  DeviceConfiguration,
  DeviceConfigurationInput,
  Actuator,
  CreateActuator,
  UpdateActuatorPayload,
  Alert,
  AlertRule,
  CreateAlertRule,
  ApiResponse,
  PagedResult,
} from '../types';
import { runtimeConfig } from '../config/runtimeConfig';

const API_BASE_URL = runtimeConfig.apiBaseUrl;

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
    try {
      const response = await this.api.get<ApiResponse<DeviceList[]>>('/devices');
      
      // Handle both camelCase and PascalCase responses (for compatibility)
      const apiResponse = response.data as any;
      const success = apiResponse.success ?? apiResponse.Success ?? false;
      const data = apiResponse.data ?? apiResponse.Data;
      
      if (success && data) {
        return Array.isArray(data) ? data : [];
      }
      
      console.warn('API returned unsuccessful response:', apiResponse);
      return [];
    } catch (error: any) {
      console.error('Error fetching devices:', error.message || error);
      if (error.response) {
        console.error('Response status:', error.response.status);
        console.error('Response data:', error.response.data);
      } else if (error.request) {
        console.error('No response received. Is the backend running at', API_BASE_URL, '?');
      }
      throw error;
    }
  }

  async getDevice(id: number): Promise<Device> {
    const response = await this.api.get<ApiResponse<Device>>(`/devices/${id}`);
    if (!response.data.data) {
      throw new Error('Device not found');
    }
    return response.data.data;
  }

  async createDevice(device: CreateDevice): Promise<Device> {
    try {
      const response = await this.api.post<ApiResponse<Device>>('/devices', device);
      if (!response.data.data) {
        const errorMessage = response.data.errors?.join(', ') || response.data.message || 'Failed to create device';
        throw new Error(errorMessage);
      }
      return response.data.data;
    } catch (error: any) {
      if (error.response?.data) {
        const apiError = error.response.data as ApiResponse<Device>;
        const errorMessage = apiError.errors?.join(', ') || apiError.message || error.message || 'Failed to create device';
        throw new Error(errorMessage);
      }
      throw error;
    }
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

  async updateSensor(id: number, sensor: UpdateSensorPayload): Promise<Sensor> {
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

  // Device Command endpoints
  async createDeviceCommand(deviceId: number, command: CreateDeviceCommand): Promise<DeviceCommand> {
    const response = await this.api.post<ApiResponse<DeviceCommand>>(
      `/devices/${deviceId}/commands`,
      command
    );
    if (!response.data.data) {
      throw new Error(response.data.message || 'Failed to create device command');
    }
    return response.data.data;
  }

  async getDeviceCommands(
    deviceId: number,
    params?: {
      status?: string;
      startDate?: string;
      endDate?: string;
      pageNumber?: number;
      pageSize?: number;
      actuatorId?: number;
    }
  ): Promise<PagedResult<DeviceCommand>> {
    const response = await this.api.get<ApiResponse<PagedResult<DeviceCommand>>>(
      `/devices/${deviceId}/commands`,
      { params }
    );

    if (!response.data.data) {
      throw new Error('Failed to get device commands');
    }

    return response.data.data;
  }

  // Actuators (outputs per device)
  async getActuatorsByDevice(deviceId: number): Promise<Actuator[]> {
    const response = await this.api.get<ApiResponse<Actuator[]>>(
      `/actuators/devices/${deviceId}/actuators`
    );
    return response.data.data || [];
  }

  async createActuator(deviceId: number, body: CreateActuator): Promise<Actuator> {
    const response = await this.api.post<ApiResponse<Actuator>>(
      `/actuators/devices/${deviceId}/actuators`,
      body
    );
    if (!response.data.data) {
      throw new Error(response.data.message || 'Failed to create actuator');
    }
    return response.data.data;
  }

  async updateActuator(
    deviceId: number,
    actuatorId: number,
    body: UpdateActuatorPayload
  ): Promise<Actuator> {
    const response = await this.api.put<ApiResponse<Actuator>>(
      `/actuators/devices/${deviceId}/actuators/${actuatorId}`,
      body
    );
    if (!response.data.data) {
      throw new Error(response.data.message || 'Failed to update actuator');
    }
    return response.data.data;
  }

  async deleteActuator(deviceId: number, actuatorId: number): Promise<void> {
    await this.api.delete(`/actuators/devices/${deviceId}/actuators/${actuatorId}`);
  }

  // Device Configuration endpoints
  async getDeviceConfigurations(deviceId: number): Promise<DeviceConfiguration[]> {
    const response = await this.api.get<ApiResponse<DeviceConfiguration[]>>(
      `/devices/${deviceId}/configurations`
    );
    return response.data.data || [];
  }

  async updateDeviceConfigurations(
    deviceId: number,
    configurations: DeviceConfigurationInput[]
  ): Promise<DeviceConfiguration[]> {
    const response = await this.api.put<ApiResponse<DeviceConfiguration[]>>(
      `/devices/${deviceId}/configurations`,
      { configurations }
    );
    if (!response.data.data) {
      throw new Error(response.data.message || 'Failed to update device configurations');
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

  // Health check endpoints
  async checkHealth(): Promise<{ status: string; timestamp: string }> {
    try {
      const response = await this.api.get<ApiResponse<{ status: string; timestamp: string }>>('/health');
      if (response.data.data) {
        return response.data.data;
      }
      throw new Error('Health check failed');
    } catch (error: any) {
      console.error('Error checking API health:', error.message || error);
      throw error;
    }
  }

  async checkMqttBrokerStatus(): Promise<{
    status: string;
    host: string;
    port: number;
    accessible: boolean;
    mqttReady: boolean;
    timestamp: string;
  }> {
    try {
      const response = await this.api.get<ApiResponse<{
        status: string;
        host: string;
        port: number;
        accessible: boolean;
        mqttReady: boolean;
        timestamp: string;
      }>>('/health/mqtt');
      
      if (response.data.data) {
        return response.data.data;
      }
      throw new Error('MQTT broker status check failed');
    } catch (error: any) {
      console.error('Error checking MQTT broker status:', error.message || error);
      if (error.response) {
        console.error('Response status:', error.response.status);
        console.error('Response data:', error.response.data);
      } else if (error.request) {
        console.error('No response received. Is the backend running at', API_BASE_URL, '?');
      }
      throw error;
    }
  }
}

export const apiService = new ApiService();

