// DTOs matching backend
export interface Device {
  deviceId: number;
  deviceName: string;
  deviceType: string;
  location?: string;
  facilityType?: string;
  edgeDeviceType?: string;
  edgeDeviceId?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  lastSeenAt?: string;
  description?: string;
}

export interface DeviceList {
  deviceId: number;
  deviceName: string;
  deviceType: string;
  location?: string;
  isActive: boolean;
  lastSeenAt?: string;
}

export interface CreateDevice {
  deviceName: string;
  deviceType: string;
  location?: string;
  facilityType?: string;
  edgeDeviceType?: string;
  edgeDeviceId?: string;
  description?: string;
}

export interface Sensor {
  sensorId: number;
  deviceId: number;
  edgeDeviceId?: string;
  sensorName: string;
  sensorType: string;
  unit?: string;
  minValue?: number;
  maxValue?: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateSensor {
  sensorName: string;
  sensorType: string;
  unit?: string;
  minValue?: number;
  maxValue?: number;
}

export interface SensorReading {
  readingId: number;
  deviceId: number;
  sensorId: number;
  value: number;
  timestamp: string;
  status?: string;
  quality?: string;
  createdAt: string;
}

export interface CreateSensorReading {
  deviceId: number;
  sensorId: number;
  value: number;
  timestamp?: string;
  status?: string;
  quality?: string;
}

export interface Alert {
  alertId: number;
  alertRuleId: number;
  deviceId: number;
  sensorId?: number;
  severity: string;
  message: string;
  triggerValue?: number;
  status: string;
  triggeredAt: string;
  acknowledgedAt?: string;
  resolvedAt?: string;
  createdAt: string;
}

export interface AlertRule {
  alertRuleId: number;
  deviceId?: number;
  sensorId?: number;
  ruleName: string;
  ruleType: string;
  condition: string;
  thresholdValue?: number;
  comparisonOperator?: string;
  severity: string;
  isEnabled: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateAlertRule {
  deviceId?: number;
  sensorId?: number;
  ruleName: string;
  ruleType: string;
  condition: string;
  thresholdValue?: number;
  comparisonOperator?: string;
  severity: string;
  isEnabled: boolean;
}

export interface ApiResponse<T> {
  success: boolean;
  message?: string;
  data?: T;
  errors?: string[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

