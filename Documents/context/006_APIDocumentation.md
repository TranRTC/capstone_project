# API Documentation

## Document Information
- **Project:** Web-Based IoT Device Real-Time Monitoring System
- **Version:** 1.0
- **Date:** 2026
- **Status:** Draft
- **Base URL:** `https://api.iotmonitoring.com/api/v1` (Development: `https://localhost:5001/api/v1`)

## 1. Introduction

### 1.1 Purpose
This document provides comprehensive API documentation for the Web-Based IoT Device Real-Time Monitoring System. It includes detailed specifications for all RESTful API endpoints, request/response formats, error handling, and integration examples.

### 1.2 API Overview
The API follows RESTful principles and uses JSON for data exchange. All endpoints return standard HTTP status codes and consistent response formats.

### 1.3 Base URL
```
Production: https://api.iotmonitoring.com/api/v1
Development: https://localhost:5001/api/v1
```

### 1.4 Authentication
**Note:** For capstone project, authentication is simplified. In production, implement proper authentication (JWT tokens, OAuth, etc.).

### 1.5 Response Format
All API responses follow a standard format:

```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": { ... },
  "errors": null
}
```

Error responses:
```json
{
  "success": false,
  "message": "An error occurred",
  "data": null,
  "errors": ["Error detail 1", "Error detail 2"]
}
```

## 2. HTTP Status Codes

| Code | Description |
|------|-------------|
| 200 | OK - Request successful |
| 201 | Created - Resource created successfully |
| 204 | No Content - Request successful, no content to return |
| 400 | Bad Request - Invalid request parameters |
| 401 | Unauthorized - Authentication required |
| 403 | Forbidden - Insufficient permissions |
| 404 | Not Found - Resource not found |
| 500 | Internal Server Error - Server error |

## 3. Device Management Endpoints

### 3.1 Get All Devices

**Endpoint:** `GET /devices`

**Description:** Retrieves a list of all registered devices.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| isActive | boolean | No | Filter by active status |
| deviceType | string | No | Filter by device type |
| location | string | No | Filter by location |

**Request Example:**
```http
GET /api/v1/devices?isActive=true&deviceType=Motor HTTP/1.1
Host: localhost:5001
```

**Response Example (200 OK):**
```json
{
  "success": true,
  "message": "Devices retrieved successfully",
  "data": [
    {
      "deviceId": 1,
      "deviceName": "Motor-001",
      "deviceType": "Motor",
      "location": "Building A, Floor 2",
      "facilityType": "Industrial",
      "edgeDeviceType": "Raspberry Pi",
      "edgeDeviceId": "RPI-001",
      "isActive": true,
      "createdAt": "2026-01-15T10:30:00Z",
      "updatedAt": "2026-01-15T10:30:00Z",
      "lastSeenAt": "2026-01-20T14:25:00Z",
      "description": "Main motor for production line"
    },
    {
      "deviceId": 2,
      "deviceName": "Temperature Sensor-001",
      "deviceType": "Temperature Sensor",
      "location": "Building B, Room 101",
      "facilityType": "Commercial",
      "edgeDeviceType": "ESP32",
      "edgeDeviceId": "ESP-001",
      "isActive": true,
      "createdAt": "2026-01-16T09:15:00Z",
      "updatedAt": "2026-01-16T09:15:00Z",
      "lastSeenAt": "2026-01-20T14:24:00Z",
      "description": "Room temperature monitoring"
    }
  ],
  "errors": null
}
```

### 3.2 Get Device by ID

**Endpoint:** `GET /devices/{id}`

**Description:** Retrieves detailed information for a specific device.

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | integer | Yes | Device ID |

**Request Example:**
```http
GET /api/v1/devices/1 HTTP/1.1
Host: localhost:5001
```

**Response Example (200 OK):**
```json
{
  "success": true,
  "message": "Device retrieved successfully",
  "data": {
    "deviceId": 1,
    "deviceName": "Motor-001",
    "deviceType": "Motor",
    "location": "Building A, Floor 2",
    "facilityType": "Industrial",
    "edgeDeviceType": "Raspberry Pi",
    "edgeDeviceId": "RPI-001",
    "isActive": true,
    "createdAt": "2026-01-15T10:30:00Z",
    "updatedAt": "2026-01-15T10:30:00Z",
    "lastSeenAt": "2026-01-20T14:25:00Z",
    "description": "Main motor for production line",
    "sensors": [
      {
        "sensorId": 1,
        "sensorName": "Temperature",
        "sensorType": "Temperature",
        "unit": "°C"
      }
    ]
  },
  "errors": null
}
```

**Error Response (404 Not Found):**
```json
{
  "success": false,
  "message": "Device not found",
  "data": null,
  "errors": ["Device with ID 999 does not exist"]
}
```

### 3.3 Create Device

**Endpoint:** `POST /devices`

**Description:** Creates a new device registration.

**Request Body:**
```json
{
  "deviceName": "Motor-002",
  "deviceType": "Motor",
  "location": "Building A, Floor 3",
  "facilityType": "Industrial",
  "edgeDeviceType": "Raspberry Pi",
  "edgeDeviceId": "RPI-002",
  "description": "Secondary motor for production line"
}
```

**Request Example:**
```http
POST /api/v1/devices HTTP/1.1
Host: localhost:5001
Content-Type: application/json

{
  "deviceName": "Motor-002",
  "deviceType": "Motor",
  "location": "Building A, Floor 3",
  "facilityType": "Industrial",
  "edgeDeviceType": "Raspberry Pi",
  "edgeDeviceId": "RPI-002",
  "description": "Secondary motor for production line"
}
```

**Response Example (201 Created):**
```json
{
  "success": true,
  "message": "Device created successfully",
  "data": {
    "deviceId": 3,
    "deviceName": "Motor-002",
    "deviceType": "Motor",
    "location": "Building A, Floor 3",
    "facilityType": "Industrial",
    "edgeDeviceType": "Raspberry Pi",
    "edgeDeviceId": "RPI-002",
    "isActive": true,
    "createdAt": "2026-01-20T15:00:00Z",
    "updatedAt": "2026-01-20T15:00:00Z",
    "lastSeenAt": null,
    "description": "Secondary motor for production line"
  },
  "errors": null
}
```

**Error Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "Validation failed",
  "data": null,
  "errors": [
    "DeviceName is required",
    "DeviceType is required"
  ]
}
```

### 3.4 Update Device

**Endpoint:** `PUT /devices/{id}`

**Description:** Updates an existing device.

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | integer | Yes | Device ID |

**Request Body:**
```json
{
  "deviceName": "Motor-001-Updated",
  "location": "Building A, Floor 2, Room 205",
  "description": "Updated description",
  "isActive": true
}
```

**Request Example:**
```http
PUT /api/v1/devices/1 HTTP/1.1
Host: localhost:5001
Content-Type: application/json

{
  "deviceName": "Motor-001-Updated",
  "location": "Building A, Floor 2, Room 205",
  "description": "Updated description",
  "isActive": true
}
```

**Response Example (200 OK):**
```json
{
  "success": true,
  "message": "Device updated successfully",
  "data": {
    "deviceId": 1,
    "deviceName": "Motor-001-Updated",
    "deviceType": "Motor",
    "location": "Building A, Floor 2, Room 205",
    "facilityType": "Industrial",
    "edgeDeviceType": "Raspberry Pi",
    "edgeDeviceId": "RPI-001",
    "isActive": true,
    "createdAt": "2026-01-15T10:30:00Z",
    "updatedAt": "2026-01-20T15:30:00Z",
    "lastSeenAt": "2026-01-20T14:25:00Z",
    "description": "Updated description"
  },
  "errors": null
}
```

### 3.5 Delete Device

**Endpoint:** `DELETE /devices/{id}`

**Description:** Deletes a device (soft delete by setting isActive to false, or hard delete).

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | integer | Yes | Device ID |

**Request Example:**
```http
DELETE /api/v1/devices/1 HTTP/1.1
Host: localhost:5001
```

**Response Example (204 No Content):**
```
(No response body)
```

### 3.6 Get Device Status

**Endpoint:** `GET /devices/{id}/status`

**Description:** Retrieves the current status of a device.

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | integer | Yes | Device ID |

**Request Example:**
```http
GET /api/v1/devices/1/status HTTP/1.1
Host: localhost:5001
```

**Response Example (200 OK):**
```json
{
  "success": true,
  "message": "Device status retrieved successfully",
  "data": {
    "deviceId": 1,
    "status": "Online",
    "previousStatus": "Connected",
    "statusCode": 1,
    "message": "Device is online and sending data",
    "timestamp": "2026-01-20T14:25:00Z",
    "lastSeenAt": "2026-01-20T14:25:00Z"
  },
  "errors": null
}
```

## 4. Sensor Management Endpoints

### 4.1 Get Sensors by Device

**Endpoint:** `GET /devices/{deviceId}/sensors`

**Description:** Retrieves all sensors associated with a device.

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| deviceId | integer | Yes | Device ID |

**Request Example:**
```http
GET /api/v1/devices/1/sensors HTTP/1.1
Host: localhost:5001
```

**Response Example (200 OK):**
```json
{
  "success": true,
  "message": "Sensors retrieved successfully",
  "data": [
    {
      "sensorId": 1,
      "deviceId": 1,
      "edgeDeviceId": "RPI-001",
      "sensorName": "Temperature",
      "sensorType": "Temperature",
      "unit": "°C",
      "minValue": -40.0,
      "maxValue": 85.0,
      "isActive": true,
      "createdAt": "2026-01-15T10:35:00Z",
      "updatedAt": "2026-01-15T10:35:00Z"
    },
    {
      "sensorId": 2,
      "deviceId": 1,
      "edgeDeviceId": "RPI-001",
      "sensorName": "Vibration",
      "sensorType": "Vibration",
      "unit": "g",
      "minValue": 0.0,
      "maxValue": 16.0,
      "isActive": true,
      "createdAt": "2026-01-15T10:36:00Z",
      "updatedAt": "2026-01-15T10:36:00Z"
    }
  ],
  "errors": null
}
```

### 4.2 Get Sensor by ID

**Endpoint:** `GET /sensors/{id}`

**Description:** Retrieves detailed information for a specific sensor.

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | integer | Yes | Sensor ID |

**Request Example:**
```http
GET /api/v1/sensors/1 HTTP/1.1
Host: localhost:5001
```

**Response Example (200 OK):**
```json
{
  "success": true,
  "message": "Sensor retrieved successfully",
  "data": {
    "sensorId": 1,
    "deviceId": 1,
    "edgeDeviceId": "RPI-001",
    "sensorName": "Temperature",
    "sensorType": "Temperature",
    "unit": "°C",
    "minValue": -40.0,
    "maxValue": 85.0,
    "isActive": true,
    "createdAt": "2026-01-15T10:35:00Z",
    "updatedAt": "2026-01-15T10:35:00Z",
    "device": {
      "deviceId": 1,
      "deviceName": "Motor-001",
      "deviceType": "Motor"
    }
  },
  "errors": null
}
```

### 4.3 Create Sensor

**Endpoint:** `POST /devices/{deviceId}/sensors`

**Description:** Creates a new sensor for a device.

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| deviceId | integer | Yes | Device ID |

**Request Body:**
```json
{
  "edgeDeviceId": "RPI-001",
  "sensorName": "Humidity",
  "sensorType": "Humidity",
  "unit": "%",
  "minValue": 0.0,
  "maxValue": 100.0
}
```

**Request Example:**
```http
POST /api/v1/devices/1/sensors HTTP/1.1
Host: localhost:5001
Content-Type: application/json

{
  "edgeDeviceId": "RPI-001",
  "sensorName": "Humidity",
  "sensorType": "Humidity",
  "unit": "%",
  "minValue": 0.0,
  "maxValue": 100.0
}
```

**Response Example (201 Created):**
```json
{
  "success": true,
  "message": "Sensor created successfully",
  "data": {
    "sensorId": 3,
    "deviceId": 1,
    "edgeDeviceId": "RPI-001",
    "sensorName": "Humidity",
    "sensorType": "Humidity",
    "unit": "%",
    "minValue": 0.0,
    "maxValue": 100.0,
    "isActive": true,
    "createdAt": "2026-01-20T16:00:00Z",
    "updatedAt": "2026-01-20T16:00:00Z"
  },
  "errors": null
}
```

### 4.4 Update Sensor

**Endpoint:** `PUT /sensors/{id}`

**Description:** Updates an existing sensor.

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | integer | Yes | Sensor ID |

**Request Body:**
```json
{
  "sensorName": "Temperature-Updated",
  "minValue": -50.0,
  "maxValue": 100.0,
  "isActive": true
}
```

**Response Example (200 OK):**
```json
{
  "success": true,
  "message": "Sensor updated successfully",
  "data": {
    "sensorId": 1,
    "deviceId": 1,
    "edgeDeviceId": "RPI-001",
    "sensorName": "Temperature-Updated",
    "sensorType": "Temperature",
    "unit": "°C",
    "minValue": -50.0,
    "maxValue": 100.0,
    "isActive": true,
    "createdAt": "2026-01-15T10:35:00Z",
    "updatedAt": "2026-01-20T16:30:00Z"
  },
  "errors": null
}
```

### 4.5 Delete Sensor

**Endpoint:** `DELETE /sensors/{id}`

**Description:** Deletes a sensor.

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | integer | Yes | Sensor ID |

**Response Example (204 No Content):**
```
(No response body)
```

## 5. Sensor Readings Endpoints

### 5.1 Create Sensor Reading

**Endpoint:** `POST /sensorreadings`

**Description:** Creates a new sensor reading. This is the primary endpoint used by edge devices to send sensor data.

**Request Body:**
```json
{
  "deviceId": 1,
  "sensorId": 1,
  "value": 25.5,
  "timestamp": "2026-01-20T14:30:00Z",
  "status": "Good",
  "quality": "High"
}
```

**Request Example:**
```http
POST /api/v1/sensorreadings HTTP/1.1
Host: localhost:5001
Content-Type: application/json

{
  "deviceId": 1,
  "sensorId": 1,
  "value": 25.5,
  "timestamp": "2026-01-20T14:30:00Z",
  "status": "Good",
  "quality": "High"
}
```

**Response Example (201 Created):**
```json
{
  "success": true,
  "message": "Sensor reading created successfully",
  "data": {
    "readingId": 12345,
    "deviceId": 1,
    "sensorId": 1,
    "value": 25.5,
    "timestamp": "2026-01-20T14:30:00Z",
    "status": "Good",
    "quality": "High",
    "createdAt": "2026-01-20T14:30:01Z"
  },
  "errors": null
}
```

**Note:** If `timestamp` is not provided, the server will use the current UTC time.

### 5.2 Batch Create Sensor Readings

**Endpoint:** `POST /sensorreadings/batch`

**Description:** Creates multiple sensor readings in a single request. Useful for edge devices sending multiple readings at once.

**Request Body:**
```json
{
  "readings": [
    {
      "deviceId": 1,
      "sensorId": 1,
      "value": 25.5,
      "timestamp": "2026-01-20T14:30:00Z"
    },
    {
      "deviceId": 1,
      "sensorId": 2,
      "value": 0.5,
      "timestamp": "2026-01-20T14:30:00Z"
    }
  ]
}
```

**Request Example:**
```http
POST /api/v1/sensorreadings/batch HTTP/1.1
Host: localhost:5001
Content-Type: application/json

{
  "readings": [
    {
      "deviceId": 1,
      "sensorId": 1,
      "value": 25.5,
      "timestamp": "2026-01-20T14:30:00Z"
    },
    {
      "deviceId": 1,
      "sensorId": 2,
      "value": 0.5,
      "timestamp": "2026-01-20T14:30:00Z"
    }
  ]
}
```

**Response Example (201 Created):**
```json
{
  "success": true,
  "message": "Sensor readings created successfully",
  "data": {
    "createdCount": 2,
    "readings": [
      {
        "readingId": 12345,
        "deviceId": 1,
        "sensorId": 1,
        "value": 25.5,
        "timestamp": "2026-01-20T14:30:00Z"
      },
      {
        "readingId": 12346,
        "deviceId": 1,
        "sensorId": 2,
        "value": 0.5,
        "timestamp": "2026-01-20T14:30:00Z"
      }
    ]
  },
  "errors": null
}
```

### 5.3 Query Sensor Readings

**Endpoint:** `GET /sensorreadings`

**Description:** Retrieves sensor readings with filtering and pagination.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| deviceId | integer | No | Filter by device ID |
| sensorId | integer | No | Filter by sensor ID |
| startDate | datetime | No | Start date (ISO 8601) |
| endDate | datetime | No | End date (ISO 8601) |
| pageNumber | integer | No | Page number (default: 1) |
| pageSize | integer | No | Page size (default: 100, max: 1000) |

**Request Example:**
```http
GET /api/v1/sensorreadings?deviceId=1&sensorId=1&startDate=2026-01-20T00:00:00Z&endDate=2026-01-20T23:59:59Z&pageNumber=1&pageSize=50 HTTP/1.1
Host: localhost:5001
```

**Response Example (200 OK):**
```json
{
  "success": true,
  "message": "Sensor readings retrieved successfully",
  "data": {
    "items": [
      {
        "readingId": 12340,
        "deviceId": 1,
        "sensorId": 1,
        "value": 25.2,
        "timestamp": "2026-01-20T14:25:00Z",
        "status": "Good",
        "quality": "High"
      },
      {
        "readingId": 12341,
        "deviceId": 1,
        "sensorId": 1,
        "value": 25.3,
        "timestamp": "2026-01-20T14:26:00Z",
        "status": "Good",
        "quality": "High"
      }
    ],
    "totalCount": 1440,
    "pageNumber": 1,
    "pageSize": 50,
    "totalPages": 29
  },
  "errors": null
}
```

### 5.4 Get Readings by Device

**Endpoint:** `GET /devices/{deviceId}/readings`

**Description:** Retrieves all sensor readings for a specific device.

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| deviceId | integer | Yes | Device ID |

**Query Parameters:** Same as Query Sensor Readings (startDate, endDate, pageNumber, pageSize)

**Request Example:**
```http
GET /api/v1/devices/1/readings?startDate=2026-01-20T00:00:00Z&endDate=2026-01-20T23:59:59Z HTTP/1.1
Host: localhost:5001
```

**Response Example (200 OK):**
```json
{
  "success": true,
  "message": "Sensor readings retrieved successfully",
  "data": {
    "items": [
      {
        "readingId": 12340,
        "deviceId": 1,
        "sensorId": 1,
        "value": 25.2,
        "timestamp": "2026-01-20T14:25:00Z"
      }
    ],
    "totalCount": 2880,
    "pageNumber": 1,
    "pageSize": 100,
    "totalPages": 29
  },
  "errors": null
}
```

### 5.5 Get Readings by Sensor

**Endpoint:** `GET /sensors/{sensorId}/readings`

**Description:** Retrieves all sensor readings for a specific sensor.

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| sensorId | integer | Yes | Sensor ID |

**Query Parameters:** Same as Query Sensor Readings

**Request Example:**
```http
GET /api/v1/sensors/1/readings?startDate=2026-01-20T00:00:00Z&endDate=2026-01-20T23:59:59Z HTTP/1.1
Host: localhost:5001
```

**Response Example (200 OK):**
```json
{
  "success": true,
  "message": "Sensor readings retrieved successfully",
  "data": {
    "items": [
      {
        "readingId": 12340,
        "deviceId": 1,
        "sensorId": 1,
        "value": 25.2,
        "timestamp": "2026-01-20T14:25:00Z"
      }
    ],
    "totalCount": 1440,
    "pageNumber": 1,
    "pageSize": 100,
    "totalPages": 15
  },
  "errors": null
}
```

## 6. Alert Management Endpoints

### 6.1 Get All Alerts

**Endpoint:** `GET /alerts`

**Description:** Retrieves all alerts with filtering and pagination.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| status | string | No | Filter by status (Active, Acknowledged, Resolved) |
| severity | string | No | Filter by severity (Low, Medium, High, Critical) |
| deviceId | integer | No | Filter by device ID |
| startDate | datetime | No | Start date |
| endDate | datetime | No | End date |
| pageNumber | integer | No | Page number (default: 1) |
| pageSize | integer | No | Page size (default: 50) |

**Request Example:**
```http
GET /api/v1/alerts?status=Active&severity=High&pageNumber=1&pageSize=20 HTTP/1.1
Host: localhost:5001
```

**Response Example (200 OK):**
```json
{
  "success": true,
  "message": "Alerts retrieved successfully",
  "data": {
    "items": [
      {
        "alertId": 1001,
        "alertRuleId": 5,
        "deviceId": 1,
        "sensorId": 1,
        "severity": "High",
        "message": "Temperature exceeded threshold: 85.5°C (threshold: 80°C)",
        "triggerValue": 85.5,
        "status": "Active",
        "triggeredAt": "2026-01-20T14:30:00Z",
        "acknowledgedAt": null,
        "resolvedAt": null,
        "createdAt": "2026-01-20T14:30:01Z"
      }
    ],
    "totalCount": 15,
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 1
  },
  "errors": null
}
```

### 6.2 Get Alert by ID

**Endpoint:** `GET /alerts/{id}`

**Description:** Retrieves detailed information for a specific alert.

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | integer | Yes | Alert ID |

**Request Example:**
```http
GET /api/v1/alerts/1001 HTTP/1.1
Host: localhost:5001
```

**Response Example (200 OK):**
```json
{
  "success": true,
  "message": "Alert retrieved successfully",
  "data": {
    "alertId": 1001,
    "alertRuleId": 5,
    "deviceId": 1,
    "sensorId": 1,
    "severity": "High",
    "message": "Temperature exceeded threshold: 85.5°C (threshold: 80°C)",
    "triggerValue": 85.5,
    "status": "Active",
    "triggeredAt": "2026-01-20T14:30:00Z",
    "acknowledgedAt": null,
    "resolvedAt": null,
    "createdAt": "2026-01-20T14:30:01Z",
    "alertRule": {
      "alertRuleId": 5,
      "ruleName": "High Temperature Alert",
      "ruleType": "Threshold",
      "thresholdValue": 80.0
    },
    "device": {
      "deviceId": 1,
      "deviceName": "Motor-001"
    }
  },
  "errors": null
}
```

### 6.3 Get Active Alerts

**Endpoint:** `GET /alerts/active`

**Description:** Retrieves all active (unresolved) alerts.

**Request Example:**
```http
GET /api/v1/alerts/active HTTP/1.1
Host: localhost:5001
```

**Response Example (200 OK):**
```json
{
  "success": true,
  "message": "Active alerts retrieved successfully",
  "data": [
    {
      "alertId": 1001,
      "deviceId": 1,
      "severity": "High",
      "message": "Temperature exceeded threshold",
      "status": "Active",
      "triggeredAt": "2026-01-20T14:30:00Z"
    }
  ],
  "errors": null
}
```

### 6.4 Acknowledge Alert

**Endpoint:** `PUT /alerts/{id}/acknowledge`

**Description:** Marks an alert as acknowledged.

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | integer | Yes | Alert ID |

**Request Example:**
```http
PUT /api/v1/alerts/1001/acknowledge HTTP/1.1
Host: localhost:5001
```

**Response Example (200 OK):**
```json
{
  "success": true,
  "message": "Alert acknowledged successfully",
  "data": {
    "alertId": 1001,
    "status": "Acknowledged",
    "acknowledgedAt": "2026-01-20T15:00:00Z"
  },
  "errors": null
}
```

### 6.5 Resolve Alert

**Endpoint:** `PUT /alerts/{id}/resolve`

**Description:** Marks an alert as resolved.

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | integer | Yes | Alert ID |

**Request Example:**
```http
PUT /api/v1/alerts/1001/resolve HTTP/1.1
Host: localhost:5001
```

**Response Example (200 OK):**
```json
{
  "success": true,
  "message": "Alert resolved successfully",
  "data": {
    "alertId": 1001,
    "status": "Resolved",
    "resolvedAt": "2026-01-20T15:30:00Z"
  },
  "errors": null
}
```

## 7. Alert Rules Endpoints

### 7.1 Get All Alert Rules

**Endpoint:** `GET /alertrules`

**Description:** Retrieves all alert rules.

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| deviceId | integer | No | Filter by device ID |
| sensorId | integer | No | Filter by sensor ID |
| isEnabled | boolean | No | Filter by enabled status |

**Request Example:**
```http
GET /api/v1/alertrules?deviceId=1&isEnabled=true HTTP/1.1
Host: localhost:5001
```

**Response Example (200 OK):**
```json
{
  "success": true,
  "message": "Alert rules retrieved successfully",
  "data": [
    {
      "alertRuleId": 5,
      "deviceId": 1,
      "sensorId": 1,
      "ruleName": "High Temperature Alert",
      "ruleType": "Threshold",
      "condition": "Temperature exceeds threshold",
      "thresholdValue": 80.0,
      "comparisonOperator": ">",
      "severity": "High",
      "isEnabled": true,
      "createdAt": "2026-01-15T10:00:00Z",
      "updatedAt": "2026-01-15T10:00:00Z"
    }
  ],
  "errors": null
}
```

### 7.2 Create Alert Rule

**Endpoint:** `POST /alertrules`

**Description:** Creates a new alert rule.

**Request Body:**
```json
{
  "deviceId": 1,
  "sensorId": 1,
  "ruleName": "Low Temperature Alert",
  "ruleType": "Threshold",
  "condition": "Temperature below threshold",
  "thresholdValue": 10.0,
  "comparisonOperator": "<",
  "severity": "Medium"
}
```

**Request Example:**
```http
POST /api/v1/alertrules HTTP/1.1
Host: localhost:5001
Content-Type: application/json

{
  "deviceId": 1,
  "sensorId": 1,
  "ruleName": "Low Temperature Alert",
  "ruleType": "Threshold",
  "condition": "Temperature below threshold",
  "thresholdValue": 10.0,
  "comparisonOperator": "<",
  "severity": "Medium"
}
```

**Response Example (201 Created):**
```json
{
  "success": true,
  "message": "Alert rule created successfully",
  "data": {
    "alertRuleId": 6,
    "deviceId": 1,
    "sensorId": 1,
    "ruleName": "Low Temperature Alert",
    "ruleType": "Threshold",
    "condition": "Temperature below threshold",
    "thresholdValue": 10.0,
    "comparisonOperator": "<",
    "severity": "Medium",
    "isEnabled": true,
    "createdAt": "2026-01-20T16:00:00Z",
    "updatedAt": "2026-01-20T16:00:00Z"
  },
  "errors": null
}
```

### 7.3 Update Alert Rule

**Endpoint:** `PUT /alertrules/{id}`

**Description:** Updates an existing alert rule.

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | integer | Yes | Alert Rule ID |

**Request Body:**
```json
{
  "thresholdValue": 12.0,
  "severity": "High",
  "isEnabled": true
}
```

**Response Example (200 OK):**
```json
{
  "success": true,
  "message": "Alert rule updated successfully",
  "data": {
    "alertRuleId": 6,
    "deviceId": 1,
    "sensorId": 1,
    "ruleName": "Low Temperature Alert",
    "ruleType": "Threshold",
    "condition": "Temperature below threshold",
    "thresholdValue": 12.0,
    "comparisonOperator": "<",
    "severity": "High",
    "isEnabled": true,
    "createdAt": "2026-01-20T16:00:00Z",
    "updatedAt": "2026-01-20T16:30:00Z"
  },
  "errors": null
}
```

### 7.4 Delete Alert Rule

**Endpoint:** `DELETE /alertrules/{id}`

**Description:** Deletes an alert rule.

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | integer | Yes | Alert Rule ID |

**Response Example (204 No Content):**
```
(No response body)
```

## 8. Error Handling

### 8.1 Error Response Format

All errors follow a consistent format:

```json
{
  "success": false,
  "message": "Error message",
  "data": null,
  "errors": [
    "Detailed error 1",
    "Detailed error 2"
  ]
}
```

### 8.2 Common Error Scenarios

#### 400 Bad Request - Validation Error
```json
{
  "success": false,
  "message": "Validation failed",
  "data": null,
  "errors": [
    "DeviceName is required",
    "DeviceType must be between 1 and 50 characters"
  ]
}
```

#### 404 Not Found
```json
{
  "success": false,
  "message": "Resource not found",
  "data": null,
  "errors": [
    "Device with ID 999 does not exist"
  ]
}
```

#### 500 Internal Server Error
```json
{
  "success": false,
  "message": "An error occurred while processing your request",
  "data": null,
  "errors": [
    "Internal server error. Please try again later."
  ]
}
```

## 9. Rate Limiting

**Note:** For capstone project, rate limiting may not be implemented. In production, implement rate limiting to prevent abuse.

**Recommended Limits:**
- General endpoints: 100 requests/minute per IP
- Sensor reading endpoints: 1000 requests/minute per device
- Batch endpoints: 10 requests/minute per IP

## 10. Integration Examples

### 10.1 Edge Device Integration (Python Example)

```python
import requests
import json
from datetime import datetime

API_BASE_URL = "https://localhost:5001/api/v1"

def send_sensor_reading(device_id, sensor_id, value):
    url = f"{API_BASE_URL}/sensorreadings"
    data = {
        "deviceId": device_id,
        "sensorId": sensor_id,
        "value": value,
        "timestamp": datetime.utcnow().isoformat() + "Z"
    }
    
    response = requests.post(url, json=data)
    if response.status_code == 201:
        print("Reading sent successfully")
        return response.json()
    else:
        print(f"Error: {response.status_code} - {response.text}")
        return None

# Example usage
send_sensor_reading(device_id=1, sensor_id=1, value=25.5)
```

### 10.2 Frontend Integration (JavaScript/TypeScript Example)

```typescript
// Using fetch API
async function getDevices() {
  try {
    const response = await fetch('https://localhost:5001/api/v1/devices');
    const result = await response.json();
    
    if (result.success) {
      return result.data;
    } else {
      throw new Error(result.message);
    }
  } catch (error) {
    console.error('Error fetching devices:', error);
    throw error;
  }
}

// Using Axios
import axios from 'axios';

const apiClient = axios.create({
  baseURL: 'https://localhost:5001/api/v1'
});

async function createDevice(deviceData) {
  try {
    const response = await apiClient.post('/devices', deviceData);
    return response.data.data;
  } catch (error) {
    console.error('Error creating device:', error.response?.data);
    throw error;
  }
}
```

## 11. WebSocket/SignalR Endpoints

**Note:** SignalR endpoints are not RESTful. They use WebSocket connections. See the Application Design Document (004) for SignalR hub details.

**Connection URL:** `https://localhost:5001/hubs/monitoring`

**Client Methods:**
- `SubscribeToDevice(deviceId)` - Subscribe to device updates
- `UnsubscribeFromDevice(deviceId)` - Unsubscribe from device updates
- `SubscribeToAllDevices()` - Subscribe to all device updates

**Server Events:**
- `SensorReadingReceived` - New sensor reading received
- `DeviceStatusChanged` - Device status updated
- `AlertTriggered` - New alert triggered
- `AlertAcknowledged` - Alert acknowledged
- `AlertResolved` - Alert resolved

## 12. Notes

- All timestamps are in UTC and ISO 8601 format
- All numeric values use decimal precision appropriate for the data type
- Pagination defaults: pageNumber=1, pageSize=100 (max 1000)
- Date ranges should be specified in ISO 8601 format
- For capstone project, authentication is simplified. In production, implement proper authentication/authorization
- Rate limiting may not be implemented for capstone scope

---

## Approval

- **Prepared by:** [Your Name]
- **Reviewed by:** [Reviewer Name]
- **Approved by:** [Approver Name]
- **Date:** [Date]

---

## Notes

This document is a living document and will be updated as the API evolves during development.

