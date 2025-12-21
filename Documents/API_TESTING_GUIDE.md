# API Testing Guide

## 🚀 Running the API

### Start the API Server

```bash
dotnet run --project IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj
```

The API will start on:
- **HTTPS:** `https://localhost:5001` (or similar port)
- **HTTP:** `http://localhost:5000` (or similar port)

**Note:** The exact ports will be shown in the console output when you start the API.

### Access Swagger UI

Once the API is running, open your browser and navigate to:

```
https://localhost:5001/swagger
```

or

```
http://localhost:5000/swagger
```

Swagger UI provides an interactive interface to test all API endpoints.

---

## 📋 Testing Endpoints

### 1. **Device Management Endpoints**

#### Create a Device
**POST** `/api/v1/devices`

```json
{
  "deviceName": "Temperature Sensor 01",
  "deviceType": "Temperature",
  "location": "Building A - Room 101",
  "facilityType": "Office",
  "edgeDeviceType": "ESP32",
  "edgeDeviceId": "ESP32-001",
  "description": "Main temperature monitoring sensor"
}
```

**Expected Response:**
- Status: `201 Created`
- Returns the created device with `DeviceId`

#### Get All Devices
**GET** `/api/v1/devices`

**Expected Response:**
- Status: `200 OK`
- Returns list of all devices

#### Get Device by ID
**GET** `/api/v1/devices/{id}`

Replace `{id}` with the actual device ID (e.g., `1`)

#### Update Device
**PUT** `/api/v1/devices/{id}`

```json
{
  "deviceName": "Updated Device Name",
  "isActive": true
}
```

#### Get Device Status
**GET** `/api/v1/devices/{id}/status`

---

### 2. **Sensor Management Endpoints**

#### Create a Sensor
**POST** `/api/v1/devices/{deviceId}/sensors`

```json
{
  "sensorName": "Temperature Sensor",
  "sensorType": "Temperature",
  "unit": "°C",
  "minValue": -40,
  "maxValue": 85
}
```

#### Get Sensors for Device
**GET** `/api/v1/devices/{deviceId}/sensors`

#### Get Sensor by ID
**GET** `/api/v1/sensors/{id}`

---

### 3. **Sensor Reading Endpoints**

#### Create a Sensor Reading
**POST** `/api/v1/sensorreadings`

```json
{
  "deviceId": 1,
  "sensorId": 1,
  "value": 25.5,
  "timestamp": "2025-12-20T10:30:00Z",
  "status": "Good",
  "quality": "High"
}
```

**Note:** This will:
- Save the reading to the database
- Update device's `LastSeenAt` timestamp
- Evaluate alert rules (if any match)
- Send real-time notification via SignalR

#### Batch Create Sensor Readings
**POST** `/api/v1/sensorreadings/batch`

```json
{
  "readings": [
    {
      "deviceId": 1,
      "sensorId": 1,
      "value": 25.5,
      "timestamp": "2025-12-20T10:30:00Z"
    },
    {
      "deviceId": 1,
      "sensorId": 1,
      "value": 26.0,
      "timestamp": "2025-12-20T10:31:00Z"
    }
  ]
}
```

#### Query Sensor Readings
**GET** `/api/v1/sensorreadings?deviceId=1&startDate=2025-12-20&endDate=2025-12-21&pageNumber=1&pageSize=100`

**Query Parameters:**
- `deviceId` (optional) - Filter by device
- `sensorId` (optional) - Filter by sensor
- `startDate` (optional) - Start date filter
- `endDate` (optional) - End date filter
- `pageNumber` (default: 1) - Page number
- `pageSize` (default: 100) - Items per page

#### Get Readings by Device
**GET** `/api/v1/sensorreadings/devices/{deviceId}/readings?startDate=2025-12-20&endDate=2025-12-21`

---

### 4. **Alert Management Endpoints**

#### Get All Alerts
**GET** `/api/v1/alerts?status=Active&severity=High&pageNumber=1&pageSize=50`

**Query Parameters:**
- `status` (optional) - Filter by status (Active, Resolved, etc.)
- `severity` (optional) - Filter by severity (Low, Medium, High, Critical)
- `deviceId` (optional) - Filter by device
- `startDate` (optional) - Start date filter
- `endDate` (optional) - End date filter
- `pageNumber` (default: 1)
- `pageSize` (default: 50)

#### Get Active Alerts
**GET** `/api/v1/alerts/active`

#### Get Alert by ID
**GET** `/api/v1/alerts/{id}`

#### Acknowledge Alert
**PUT** `/api/v1/alerts/{id}/acknowledge`

#### Resolve Alert
**PUT** `/api/v1/alerts/{id}/resolve`

---

### 5. **Alert Rule Management Endpoints**

#### Create Alert Rule
**POST** `/api/v1/alertrules`

```json
{
  "deviceId": 1,
  "sensorId": 1,
  "ruleName": "High Temperature Alert",
  "ruleType": "threshold",
  "condition": "Temperature exceeds threshold",
  "thresholdValue": 30.0,
  "comparisonOperator": ">",
  "severity": "High",
  "isEnabled": true
}
```

**Rule Types:**
- `threshold` - Compare value against threshold
- `range` - Check if value is within/outside range
- `change` - Detect significant changes

**Comparison Operators:**
- `>` - Greater than
- `>=` - Greater than or equal
- `<` - Less than
- `<=` - Less than or equal
- `==` - Equal to
- `!=` - Not equal to

#### Get All Alert Rules
**GET** `/api/v1/alertrules`

#### Get Alert Rule by ID
**GET** `/api/v1/alertrules/{id}`

#### Get Alert Rules by Device
**GET** `/api/v1/alertrules/devices/{deviceId}`

#### Update Alert Rule
**PUT** `/api/v1/alertrules/{id}`

```json
{
  "isEnabled": false,
  "severity": "Critical"
}
```

#### Delete Alert Rule
**DELETE** `/api/v1/alertrules/{id}`

---

## 🔌 Testing SignalR Hub

### Using Browser Console (JavaScript)

1. Open your browser's Developer Console (F12)
2. Navigate to the API URL (e.g., `https://localhost:5001`)
3. Run this JavaScript:

```javascript
// Connect to SignalR Hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:5001/monitoringhub")
    .build();

// Start connection
connection.start()
    .then(() => console.log("Connected to SignalR Hub"))
    .catch(err => console.error("Connection error:", err));

// Subscribe to device updates
connection.invoke("SubscribeToDevice", 1)
    .then(() => console.log("Subscribed to device 1"))
    .catch(err => console.error("Subscribe error:", err));

// Listen for sensor readings
connection.on("SensorReadingReceived", (reading) => {
    console.log("New sensor reading:", reading);
});

// Listen for alerts
connection.on("NewAlert", (alert) => {
    console.log("New alert triggered:", alert);
});

// Listen for alert updates
connection.on("AlertAcknowledged", (alert) => {
    console.log("Alert acknowledged:", alert);
});

connection.on("AlertResolved", (alert) => {
    console.log("Alert resolved:", alert);
});
```

**Note:** You'll need to include the SignalR client library. For testing, you can use:
```html
<script src="https://cdn.jsdelivr.net/npm/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>
```

---

## 📝 Testing Workflow Example

### Complete Testing Scenario

1. **Create a Device**
   ```
   POST /api/v1/devices
   ```

2. **Create a Sensor for the Device**
   ```
   POST /api/v1/devices/1/sensors
   ```

3. **Create an Alert Rule**
   ```
   POST /api/v1/alertrules
   ```

4. **Create Sensor Readings** (This will trigger alert evaluation)
   ```
   POST /api/v1/sensorreadings
   ```

5. **Check for Alerts**
   ```
   GET /api/v1/alerts/active
   ```

6. **Acknowledge/Resolve Alert**
   ```
   PUT /api/v1/alerts/1/acknowledge
   PUT /api/v1/alerts/1/resolve
   ```

---

## 🛠️ Using Postman or Similar Tools

### Import Collection

You can create a Postman collection with all endpoints. Here's the base URL:

```
https://localhost:5001/api/v1
```

### Headers

For most requests, you'll need:
```
Content-Type: application/json
```

### Example Postman Request

1. **Method:** POST
2. **URL:** `https://localhost:5001/api/v1/devices`
3. **Headers:**
   - `Content-Type: application/json`
4. **Body (raw JSON):**
   ```json
   {
     "deviceName": "Test Device",
     "deviceType": "Temperature",
     "location": "Test Location"
   }
   ```

---

## ⚠️ Troubleshooting

### API Not Starting
- Check if port 5000/5001 is already in use
- Verify database connection string in `appsettings.json`
- Ensure SQL Server LocalDB is running

### CORS Errors
- The API is configured to allow `http://localhost:3000` and `http://localhost:5173`
- For other origins, update `Program.cs` CORS configuration

### Database Errors
- Ensure the database migration has been applied:
  ```bash
  dotnet ef database update --project IoTMonitoringSystem.Infrastructure
  ```

### SignalR Connection Issues
- Ensure you're using the correct protocol (https/http)
- Check browser console for connection errors
- Verify the hub endpoint: `/monitoringhub`

---

## 📊 Response Format

All API responses follow this format:

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
  "message": "Error message",
  "data": null,
  "errors": ["Error detail 1", "Error detail 2"]
}
```

---

## ✅ Quick Test Checklist

- [ ] API starts without errors
- [ ] Swagger UI is accessible
- [ ] Can create a device
- [ ] Can create a sensor
- [ ] Can create sensor readings
- [ ] Can create alert rules
- [ ] Alerts are triggered when rules match
- [ ] SignalR connection works
- [ ] Real-time notifications are received

Happy Testing! 🎉

