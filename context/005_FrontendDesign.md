# Frontend Design Document

## Document Information
- **Project:** Web-Based IoT Device Real-Time Monitoring System
- **Version:** 1.0
- **Date:** 2026
- **Status:** Draft

## 1. Introduction

### 1.1 Purpose
This document describes the frontend architecture and design for the Web-Based IoT Device Real-Time Monitoring System. It defines the React application structure, component hierarchy, page layouts, state management, real-time communication, and user interface design.

### 1.2 Scope
The frontend design covers:
- React application architecture
- Component structure and hierarchy
- Page layouts and routing
- State management approach
- SignalR client integration
- API service layer
- Data visualization components
- UI/UX design patterns

### 1.3 Technology Stack
- **Framework:** React 18+
- **Language:** TypeScript / JavaScript
- **Routing:** React Router v6
- **State Management:** React Context API / Zustand (optional)
- **HTTP Client:** Axios / Fetch API
- **Real-Time:** @microsoft/signalr
- **Charts:** Chart.js / Recharts
- **UI Framework:** Material-UI (MUI) / Ant Design / Tailwind CSS
- **Build Tool:** Vite / Create React App
- **IDE:** Visual Studio Code

## 2. Application Architecture

### 2.1 Frontend Architecture Overview

```
┌─────────────────────────────────────┐
│      React Application (SPA)        │
├─────────────────────────────────────┤
│  ┌─────────────┐  ┌──────────────┐ │
│  │   Pages     │  │  Components  │ │
│  │  (Routes)   │  │  (Reusable)  │ │
│  └──────┬──────┘  └──────┬───────┘ │
│         │                │          │
│  ┌──────▼────────────────▼───────┐ │
│  │      State Management          │ │
│  │  (Context API / Zustand)      │ │
│  └──────┬────────────────────────┘ │
│         │                          │
│  ┌──────▼──────────┐  ┌──────────┐│
│  │  API Service    │  │ SignalR  ││
│  │  (Axios/Fetch)  │  │  Client  ││
│  └──────┬──────────┘  └────┬─────┘│
└─────────┼──────────────────┼──────┘
          │                  │
          ▼                  ▼
    RESTful API        SignalR Hub
    (ASP.NET Core)     (WebSocket)
```

### 2.2 Project Structure

```
iot-monitoring-frontend/
├── public/
│   ├── index.html
│   └── favicon.ico
├── src/
│   ├── components/              # Reusable components
│   │   ├── common/             # Common UI components
│   │   │   ├── Button.tsx
│   │   │   ├── Card.tsx
│   │   │   ├── Modal.tsx
│   │   │   ├── Table.tsx
│   │   │   └── LoadingSpinner.tsx
│   │   ├── charts/             # Chart components
│   │   │   ├── LineChart.tsx
│   │   │   ├── BarChart.tsx
│   │   │   ├── GaugeChart.tsx
│   │   │   └── RealTimeChart.tsx
│   │   ├── device/              # Device-related components
│   │   │   ├── DeviceCard.tsx
│   │   │   ├── DeviceStatusBadge.tsx
│   │   │   ├── DeviceList.tsx
│   │   │   └── DeviceForm.tsx
│   │   ├── sensor/              # Sensor-related components
│   │   │   ├── SensorCard.tsx
│   │   │   ├── SensorReadingDisplay.tsx
│   │   │   └── SensorList.tsx
│   │   └── alert/               # Alert-related components
│   │       ├── AlertCard.tsx
│   │       ├── AlertList.tsx
│   │       └── AlertRuleForm.tsx
│   ├── pages/                   # Page components
│   │   ├── Dashboard.tsx
│   │   ├── DeviceListPage.tsx
│   │   ├── DeviceDetailPage.tsx
│   │   ├── SensorDetailPage.tsx
│   │   ├── AlertsPage.tsx
│   │   ├── HistoricalDataPage.tsx
│   │   └── SettingsPage.tsx
│   ├── services/                # API and service layer
│   │   ├── api/
│   │   │   ├── deviceService.ts
│   │   │   ├── sensorService.ts
│   │   │   ├── readingService.ts
│   │   │   ├── alertService.ts
│   │   │   └── apiClient.ts
│   │   └── signalr/
│   │       ├── signalrService.ts
│   │       └── signalrContext.tsx
│   ├── context/                 # React Context providers
│   │   ├── DeviceContext.tsx
│   │   ├── AlertContext.tsx
│   │   └── AuthContext.tsx (if needed)
│   ├── hooks/                   # Custom React hooks
│   │   ├── useDevices.ts
│   │   ├── useSensorReadings.ts
│   │   ├── useAlerts.ts
│   │   └── useSignalR.ts
│   ├── types/                   # TypeScript types
│   │   ├── device.types.ts
│   │   ├── sensor.types.ts
│   │   ├── alert.types.ts
│   │   └── api.types.ts
│   ├── utils/                   # Utility functions
│   │   ├── formatters.ts
│   │   ├── validators.ts
│   │   └── constants.ts
│   ├── styles/                  # Global styles
│   │   ├── index.css
│   │   └── theme.ts
│   ├── App.tsx                  # Main app component
│   ├── App.css
│   ├── main.tsx                 # Entry point
│   └── router.tsx               # Route configuration
├── package.json
├── tsconfig.json
├── vite.config.ts (or similar)
└── README.md
```

## 3. Component Architecture

### 3.1 Component Hierarchy

```
App
├── Layout
│   ├── Header
│   ├── Sidebar
│   └── MainContent
│       ├── Routes
│       │   ├── Dashboard
│       │   │   ├── DeviceStatusOverview
│       │   │   ├── ActiveAlertsSummary
│       │   │   └── RealTimeCharts
│       │   ├── DeviceListPage
│       │   │   └── DeviceList
│       │   │       └── DeviceCard[]
│       │   ├── DeviceDetailPage
│       │   │   ├── DeviceInfo
│       │   │   ├── SensorList
│       │   │   │   └── SensorCard[]
│       │   │   └── RealTimeChart[]
│       │   ├── AlertsPage
│       │   │   ├── AlertFilters
│       │   │   └── AlertList
│       │   │       └── AlertCard[]
│       │   └── HistoricalDataPage
│       │       ├── DateRangePicker
│       │       └── HistoricalChart
│       └── SignalRProvider
└── ErrorBoundary
```

### 3.2 Common Components

#### 3.2.1 Button Component

```typescript
// components/common/Button.tsx
interface ButtonProps {
  variant?: 'primary' | 'secondary' | 'danger';
  size?: 'small' | 'medium' | 'large';
  onClick?: () => void;
  disabled?: boolean;
  children: React.ReactNode;
}

export const Button: React.FC<ButtonProps> = ({ 
  variant = 'primary', 
  size = 'medium',
  onClick,
  disabled,
  children 
}) => {
  // Implementation
};
```

#### 3.2.2 Card Component

```typescript
// components/common/Card.tsx
interface CardProps {
  title?: string;
  children: React.ReactNode;
  actions?: React.ReactNode;
  className?: string;
}

export const Card: React.FC<CardProps> = ({ 
  title, 
  children, 
  actions,
  className 
}) => {
  // Implementation
};
```

#### 3.2.3 LoadingSpinner Component

```typescript
// components/common/LoadingSpinner.tsx
interface LoadingSpinnerProps {
  size?: 'small' | 'medium' | 'large';
  message?: string;
}

export const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({ 
  size = 'medium',
  message 
}) => {
  // Implementation
};
```

### 3.3 Device Components

#### 3.3.1 DeviceCard Component

```typescript
// components/device/DeviceCard.tsx
interface DeviceCardProps {
  device: Device;
  onClick?: (deviceId: number) => void;
}

export const DeviceCard: React.FC<DeviceCardProps> = ({ 
  device, 
  onClick 
}) => {
  return (
    <Card onClick={() => onClick?.(device.deviceId)}>
      <DeviceStatusBadge status={device.status} />
      <h3>{device.deviceName}</h3>
      <p>{device.deviceType}</p>
      <p>{device.location}</p>
      <p>Last Seen: {formatDate(device.lastSeenAt)}</p>
    </Card>
  );
};
```

#### 3.3.2 DeviceStatusBadge Component

```typescript
// components/device/DeviceStatusBadge.tsx
interface DeviceStatusBadgeProps {
  status: 'Online' | 'Connected' | 'Offline' | 'Error' | 'Maintenance';
}

export const DeviceStatusBadge: React.FC<DeviceStatusBadgeProps> = ({ 
  status 
}) => {
  const statusColors = {
    Online: 'green',
    Connected: 'blue',
    Offline: 'gray',
    Error: 'red',
    Maintenance: 'yellow'
  };

  return (
    <Badge color={statusColors[status]}>
      {status}
    </Badge>
  );
};
```

### 3.4 Chart Components

#### 3.4.1 RealTimeChart Component

```typescript
// components/charts/RealTimeChart.tsx
interface RealTimeChartProps {
  sensorId: number;
  deviceId: number;
  title: string;
  unit?: string;
  maxDataPoints?: number;
}

export const RealTimeChart: React.FC<RealTimeChartProps> = ({
  sensorId,
  deviceId,
  title,
  unit,
  maxDataPoints = 50
}) => {
  const [data, setData] = useState<ChartDataPoint[]>([]);
  const { subscribeToSensor } = useSignalR();

  useEffect(() => {
    const unsubscribe = subscribeToSensor(deviceId, sensorId, (reading) => {
      setData(prev => {
        const newData = [...prev, {
          timestamp: reading.timestamp,
          value: reading.value
        }];
        // Keep only last N data points
        return newData.slice(-maxDataPoints);
      });
    });

    return unsubscribe;
  }, [deviceId, sensorId, subscribeToSensor]);

  return (
    <Card title={title}>
      <LineChart data={data} unit={unit} />
    </Card>
  );
};
```

#### 3.4.2 HistoricalChart Component

```typescript
// components/charts/HistoricalChart.tsx
interface HistoricalChartProps {
  sensorId: number;
  deviceId: number;
  startDate: Date;
  endDate: Date;
  title: string;
  unit?: string;
}

export const HistoricalChart: React.FC<HistoricalChartProps> = ({
  sensorId,
  deviceId,
  startDate,
  endDate,
  title,
  unit
}) => {
  const { readings, loading } = useSensorReadings({
    sensorId,
    deviceId,
    startDate,
    endDate
  });

  if (loading) return <LoadingSpinner />;

  return (
    <Card title={title}>
      <LineChart data={readings} unit={unit} />
    </Card>
  );
};
```

## 4. Page Layouts

### 4.1 Dashboard Page

**Purpose:** Main landing page showing system overview

**Layout:**
```
┌─────────────────────────────────────────┐
│  Header: IoT Monitoring System          │
├─────────────────────────────────────────┤
│  Sidebar │ Main Content                 │
│          │ ┌─────────────────────────┐  │
│          │ │ Device Status Overview  │  │
│          │ │ (Online/Offline counts) │  │
│          │ └─────────────────────────┘  │
│          │ ┌──────────┐ ┌───────────┐  │
│          │ │ Active   │ │ Recent    │  │
│          │ │ Alerts   │ │ Activity  │  │
│          │ └──────────┘ └───────────┘  │
│          │ ┌─────────────────────────┐  │
│          │ │ Real-Time Charts        │  │
│          │ │ (Top 3-5 sensors)       │  │
│          │ └─────────────────────────┘  │
└─────────────────────────────────────────┘
```

**Components:**
- DeviceStatusOverview
- ActiveAlertsSummary
- RealTimeCharts (multiple sensors)
- RecentActivityFeed

### 4.2 Device List Page

**Purpose:** Display all registered devices

**Layout:**
```
┌─────────────────────────────────────────┐
│  Header                                  │
├─────────────────────────────────────────┤
│  Sidebar │ Main Content                 │
│          │ ┌─────────────────────────┐  │
│          │ │ Search & Filters        │  │
│          │ └─────────────────────────┘  │
│          │ ┌─────────────────────────┐  │
│          │ │ Device Grid/List        │  │
│          │ │ [DeviceCard] [DeviceCard]│  │
│          │ │ [DeviceCard] [DeviceCard]│  │
│          │ └─────────────────────────┘  │
│          │ ┌─────────────────────────┐  │
│          │ │ Pagination              │  │
│          │ └─────────────────────────┘  │
└─────────────────────────────────────────┘
```

**Components:**
- DeviceSearchBar
- DeviceFilters (by type, status, location)
- DeviceList (grid or list view)
- Pagination

### 4.3 Device Detail Page

**Purpose:** Show detailed information for a single device

**Layout:**
```
┌─────────────────────────────────────────┐
│  Header                                  │
├─────────────────────────────────────────┤
│  Sidebar │ Main Content                 │
│          │ ┌─────────────────────────┐  │
│          │ │ Device Info Card        │  │
│          │ │ (Name, Type, Location)  │  │
│          │ └─────────────────────────┘  │
│          │ ┌─────────────────────────┐  │
│          │ │ Sensors List            │  │
│          │ │ [SensorCard] [SensorCard]│ │
│          │ └─────────────────────────┘  │
│          │ ┌─────────────────────────┐  │
│          │ │ Real-Time Charts        │  │
│          │ │ (One per sensor)        │  │
│          │ └─────────────────────────┘  │
└─────────────────────────────────────────┘
```

**Components:**
- DeviceInfo
- SensorList
- RealTimeChart (for each sensor)
- DeviceActions (Edit, Delete buttons)

### 4.4 Alerts Page

**Purpose:** Display and manage alerts

**Layout:**
```
┌─────────────────────────────────────────┐
│  Header                                  │
├─────────────────────────────────────────┤
│  Sidebar │ Main Content                 │
│          │ ┌─────────────────────────┐  │
│          │ │ Alert Filters           │  │
│          │ │ (Status, Severity, Date)│  │
│          │ └─────────────────────────┘  │
│          │ ┌─────────────────────────┐  │
│          │ │ Alert List              │  │
│          │ │ [AlertCard]             │  │
│          │ │ [AlertCard]             │  │
│          │ └─────────────────────────┘  │
└─────────────────────────────────────────┘
```

**Components:**
- AlertFilters
- AlertList
- AlertCard (with acknowledge/resolve actions)

### 4.5 Historical Data Page

**Purpose:** View historical sensor data with date range selection

**Layout:**
```
┌─────────────────────────────────────────┐
│  Header                                  │
├─────────────────────────────────────────┤
│  Sidebar │ Main Content                 │
│          │ ┌─────────────────────────┐  │
│          │ │ Date Range Picker       │  │
│          │ │ Device/Sensor Selector  │  │
│          │ └─────────────────────────┘  │
│          │ ┌─────────────────────────┐  │
│          │ │ Historical Chart        │  │
│          │ │ (Line/Bar chart)        │  │
│          │ └─────────────────────────┘  │
│          │ ┌─────────────────────────┐  │
│          │ │ Data Table (optional)   │  │
│          │ └─────────────────────────┘  │
└─────────────────────────────────────────┘
```

**Components:**
- DateRangePicker
- DeviceSelector
- SensorSelector
- HistoricalChart
- DataTable (optional)

## 5. Routing

### 5.1 Route Configuration

```typescript
// router.tsx
import { createBrowserRouter } from 'react-router-dom';
import Dashboard from './pages/Dashboard';
import DeviceListPage from './pages/DeviceListPage';
import DeviceDetailPage from './pages/DeviceDetailPage';
import AlertsPage from './pages/AlertsPage';
import HistoricalDataPage from './pages/HistoricalDataPage';
import SettingsPage from './pages/SettingsPage';
import Layout from './components/Layout';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    children: [
      {
        index: true,
        element: <Dashboard />
      },
      {
        path: 'devices',
        element: <DeviceListPage />
      },
      {
        path: 'devices/:deviceId',
        element: <DeviceDetailPage />
      },
      {
        path: 'alerts',
        element: <AlertsPage />
      },
      {
        path: 'historical',
        element: <HistoricalDataPage />
      },
      {
        path: 'settings',
        element: <SettingsPage />
      }
    ]
  }
]);
```

### 5.2 Navigation Structure

```
Dashboard (/)
├── Devices (/devices)
│   └── Device Detail (/devices/:id)
├── Alerts (/alerts)
├── Historical Data (/historical)
└── Settings (/settings)
```

## 6. State Management

### 6.1 Approach

For a capstone project, use **React Context API** for state management. For larger applications, consider Zustand or Redux.

### 6.2 Device Context

```typescript
// context/DeviceContext.tsx
interface DeviceContextType {
  devices: Device[];
  loading: boolean;
  error: string | null;
  selectedDevice: Device | null;
  fetchDevices: () => Promise<void>;
  selectDevice: (deviceId: number) => void;
  updateDevice: (device: Device) => void;
}

export const DeviceContext = createContext<DeviceContextType | null>(null);

export const DeviceProvider: React.FC<{ children: React.ReactNode }> = ({ 
  children 
}) => {
  const [devices, setDevices] = useState<Device[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedDevice, setSelectedDevice] = useState<Device | null>(null);

  const fetchDevices = async () => {
    setLoading(true);
    try {
      const data = await deviceService.getAllDevices();
      setDevices(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  // ... other methods

  return (
    <DeviceContext.Provider value={{
      devices,
      loading,
      error,
      selectedDevice,
      fetchDevices,
      selectDevice,
      updateDevice
    }}>
      {children}
    </DeviceContext.Provider>
  );
};
```

### 6.3 SignalR Context

```typescript
// context/SignalRContext.tsx
interface SignalRContextType {
  connection: HubConnection | null;
  isConnected: boolean;
  subscribeToDevice: (deviceId: number, callback: (data: any) => void) => () => void;
  subscribeToSensor: (deviceId: number, sensorId: number, callback: (reading: SensorReading) => void) => () => void;
}

export const SignalRContext = createContext<SignalRContextType | null>(null);

export const SignalRProvider: React.FC<{ children: React.ReactNode }> = ({ 
  children 
}) => {
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    const newConnection = new HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/monitoring`)
      .withAutomaticReconnect()
      .build();

    newConnection.start()
      .then(() => setIsConnected(true))
      .catch(err => console.error('SignalR connection error:', err));

    newConnection.onclose(() => setIsConnected(false));

    setConnection(newConnection);

    return () => {
      newConnection.stop();
    };
  }, []);

  const subscribeToDevice = (deviceId: number, callback: (data: any) => void) => {
    if (!connection) return () => {};

    connection.invoke('SubscribeToDevice', deviceId);
    connection.on('DeviceStatusChanged', callback);

    return () => {
      connection.off('DeviceStatusChanged', callback);
      connection.invoke('UnsubscribeFromDevice', deviceId);
    };
  };

  // ... other methods

  return (
    <SignalRContext.Provider value={{
      connection,
      isConnected,
      subscribeToDevice,
      subscribeToSensor
    }}>
      {children}
    </SignalRContext.Provider>
  );
};
```

## 7. API Service Layer

### 7.1 API Client Setup

```typescript
// services/api/apiClient.ts
import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'https://localhost:5001/api/v1';

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json'
  }
});

// Request interceptor
apiClient.interceptors.request.use(
  (config) => {
    // Add auth token if needed
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor
apiClient.interceptors.response.use(
  (response) => response.data,
  (error) => {
    // Handle errors
    return Promise.reject(error);
  }
);
```

### 7.2 Device Service

```typescript
// services/api/deviceService.ts
import { apiClient } from './apiClient';
import { Device, CreateDeviceDto, UpdateDeviceDto } from '../../types/device.types';

export const deviceService = {
  getAllDevices: async (): Promise<Device[]> => {
    const response = await apiClient.get<Device[]>('/devices');
    return response;
  },

  getDeviceById: async (deviceId: number): Promise<Device> => {
    const response = await apiClient.get<Device>(`/devices/${deviceId}`);
    return response;
  },

  createDevice: async (device: CreateDeviceDto): Promise<Device> => {
    const response = await apiClient.post<Device>('/devices', device);
    return response;
  },

  updateDevice: async (deviceId: number, device: UpdateDeviceDto): Promise<Device> => {
    const response = await apiClient.put<Device>(`/devices/${deviceId}`, device);
    return response;
  },

  deleteDevice: async (deviceId: number): Promise<void> => {
    await apiClient.delete(`/devices/${deviceId}`);
  }
};
```

### 7.3 Sensor Reading Service

```typescript
// services/api/readingService.ts
import { apiClient } from './apiClient';
import { SensorReading, CreateSensorReadingDto, SensorReadingQueryDto } from '../../types/sensor.types';

export const readingService = {
  createReading: async (reading: CreateSensorReadingDto): Promise<SensorReading> => {
    const response = await apiClient.post<SensorReading>('/sensorreadings', reading);
    return response;
  },

  getReadings: async (query: SensorReadingQueryDto): Promise<SensorReading[]> => {
    const response = await apiClient.get<SensorReading[]>('/sensorreadings', { params: query });
    return response;
  },

  getReadingsByDevice: async (
    deviceId: number, 
    startDate?: Date, 
    endDate?: Date
  ): Promise<SensorReading[]> => {
    const params: any = {};
    if (startDate) params.startDate = startDate.toISOString();
    if (endDate) params.endDate = endDate.toISOString();
    
    const response = await apiClient.get<SensorReading[]>(
      `/devices/${deviceId}/readings`, 
      { params }
    );
    return response;
  }
};
```

## 8. SignalR Integration

### 8.1 SignalR Service

```typescript
// services/signalr/signalrService.ts
import * as signalR from '@microsoft/signalr';
import { API_BASE_URL } from '../../utils/constants';

class SignalRService {
  private connection: signalR.HubConnection | null = null;

  async startConnection(): Promise<void> {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/monitoring`)
      .withAutomaticReconnect()
      .build();

    await this.connection.start();
  }

  async subscribeToDevice(deviceId: number): Promise<void> {
    if (this.connection) {
      await this.connection.invoke('SubscribeToDevice', deviceId);
    }
  }

  onSensorReadingReceived(callback: (reading: SensorReading) => void): void {
    if (this.connection) {
      this.connection.on('SensorReadingReceived', callback);
    }
  }

  onDeviceStatusChanged(callback: (status: DeviceStatus) => void): void {
    if (this.connection) {
      this.connection.on('DeviceStatusChanged', callback);
    }
  }

  onAlertTriggered(callback: (alert: Alert) => void): void {
    if (this.connection) {
      this.connection.on('AlertTriggered', callback);
    }
  }

  stopConnection(): void {
    if (this.connection) {
      this.connection.stop();
    }
  }
}

export const signalRService = new SignalRService();
```

### 8.2 Custom Hook for SignalR

```typescript
// hooks/useSignalR.ts
import { useEffect, useState } from 'react';
import { signalRService } from '../services/signalr/signalrService';

export const useSignalR = () => {
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    signalRService.startConnection()
      .then(() => setIsConnected(true))
      .catch(err => console.error('SignalR connection failed:', err));

    return () => {
      signalRService.stopConnection();
    };
  }, []);

  return {
    isConnected,
    subscribeToDevice: signalRService.subscribeToDevice.bind(signalRService),
    onSensorReadingReceived: signalRService.onSensorReadingReceived.bind(signalRService),
    onDeviceStatusChanged: signalRService.onDeviceStatusChanged.bind(signalRService),
    onAlertTriggered: signalRService.onAlertTriggered.bind(signalRService)
  };
};
```

## 9. TypeScript Types

### 9.1 Device Types

```typescript
// types/device.types.ts
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

export interface CreateDeviceDto {
  deviceName: string;
  deviceType: string;
  location?: string;
  facilityType?: string;
  edgeDeviceType?: string;
  edgeDeviceId?: string;
  description?: string;
}

export interface UpdateDeviceDto {
  deviceName?: string;
  deviceType?: string;
  location?: string;
  facilityType?: string;
  description?: string;
  isActive?: boolean;
}
```

### 9.2 Sensor Types

```typescript
// types/sensor.types.ts
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
```

## 10. UI/UX Design

### 10.1 Design Principles

- **Clean and Modern:** Minimalist design with clear visual hierarchy
- **Responsive:** Works on desktop (primary) and mobile (future expansion)
- **Real-Time Feedback:** Visual indicators for live data updates
- **Accessibility:** Proper contrast, keyboard navigation, screen reader support
- **Consistent:** Unified color scheme, typography, and spacing

### 10.2 Color Scheme

```typescript
// styles/theme.ts
export const theme = {
  colors: {
    primary: '#1976d2',
    secondary: '#dc004e',
    success: '#4caf50',
    warning: '#ff9800',
    error: '#f44336',
    info: '#2196f3',
    background: '#f5f5f5',
    surface: '#ffffff',
    text: '#212121',
    textSecondary: '#757575'
  },
  statusColors: {
    online: '#4caf50',
    connected: '#2196f3',
    offline: '#9e9e9e',
    error: '#f44336',
    maintenance: '#ff9800'
  }
};
```

### 10.3 Typography

- **Headings:** Bold, 16-24px
- **Body:** Regular, 14-16px
- **Labels:** Medium, 12-14px
- **Font Family:** System fonts or Roboto (Material-UI)

### 10.4 Component Spacing

- **Card Padding:** 16-24px
- **Component Gap:** 16px
- **Section Margin:** 24-32px

## 11. Data Visualization

### 11.1 Chart Library Selection

**Recommended:** Chart.js or Recharts
- Chart.js: Popular, well-documented, good performance
- Recharts: React-native, declarative API

### 11.2 Chart Types

1. **Line Chart:** Historical data, trends over time
2. **Real-Time Line Chart:** Live sensor data streaming
3. **Bar Chart:** Comparative data, aggregated metrics
4. **Gauge Chart:** Current value vs. threshold (for alerts)

### 11.3 Chart Configuration

```typescript
// Example Chart.js configuration
const chartConfig = {
  responsive: true,
  maintainAspectRatio: false,
  scales: {
    x: {
      type: 'time',
      time: {
        unit: 'minute'
      }
    },
    y: {
      title: {
        display: true,
        text: 'Value (°C)'
      }
    }
  },
  plugins: {
    legend: {
      display: true
    },
    tooltip: {
      enabled: true
    }
  }
};
```

## 12. Error Handling

### 12.1 Error Boundaries

```typescript
// components/ErrorBoundary.tsx
class ErrorBoundary extends React.Component {
  state = { hasError: false, error: null };

  static getDerivedStateFromError(error: Error) {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('Error caught by boundary:', error, errorInfo);
  }

  render() {
    if (this.state.hasError) {
      return <ErrorFallback error={this.state.error} />;
    }
    return this.props.children;
  }
}
```

### 12.2 API Error Handling

```typescript
// In API service
try {
  const data = await deviceService.getAllDevices();
  return data;
} catch (error) {
  if (error.response) {
    // Server responded with error
    throw new Error(error.response.data.message || 'An error occurred');
  } else if (error.request) {
    // Request made but no response
    throw new Error('Network error. Please check your connection.');
  } else {
    throw new Error('An unexpected error occurred.');
  }
}
```

## 13. Performance Considerations

### 13.1 Optimization Strategies

1. **Code Splitting:** Lazy load routes
2. **Memoization:** Use `React.memo`, `useMemo`, `useCallback`
3. **Virtual Scrolling:** For long lists (react-window)
4. **Debouncing:** For search/filter inputs
5. **Pagination:** Limit data fetched at once

### 13.2 Real-Time Data Optimization

- Limit data points in real-time charts (keep last 50-100 points)
- Throttle SignalR updates if needed
- Use Web Workers for heavy calculations (if needed)

## 14. Testing Strategy (Optional for Capstone)

### 14.1 Component Testing

- Use React Testing Library
- Test user interactions
- Test component rendering

### 14.2 Integration Testing

- Test API service calls
- Test SignalR connection
- Test routing

## 15. Notes

- This design follows React best practices and modern patterns
- Components are designed to be reusable and maintainable
- State management uses Context API (suitable for capstone scope)
- SignalR integration provides real-time updates
- Chart components support both real-time and historical data visualization
- UI framework choice (Material-UI, Ant Design, or Tailwind) is flexible based on preference

---

## Approval

- **Prepared by:** [Your Name]
- **Reviewed by:** [Reviewer Name]
- **Approved by:** [Approver Name]
- **Date:** [Date]

---

## Notes

This document is a living document and will be updated as the frontend design evolves during development.

