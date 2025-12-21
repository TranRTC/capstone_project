# How to Use Swagger UI

## ✅ You're All Set!

You can see your API endpoints in Swagger UI. Here's how to test them:

## Quick Testing Guide

### 1. **Expand an Endpoint**
- Click on any endpoint (like `GET /api/v1/AlertRules`)
- Click the "Try it out" button
- Fill in any required parameters
- Click "Execute"

### 2. **View the Response**
- Scroll down to see the response
- You'll see:
  - **Response Code** (200 = success, 404 = not found, etc.)
  - **Response Body** (the actual data returned)
  - **Response Headers** (metadata)

## Recommended Testing Order

### Step 1: Create a Device
1. Find **"Devices"** section
2. Click on **`POST /api/v1/Devices`**
3. Click **"Try it out"**
4. Replace the JSON with:
```json
{
  "deviceName": "Test Temperature Sensor",
  "deviceType": "Temperature",
  "location": "Test Lab",
  "facilityType": "Laboratory",
  "edgeDeviceType": "ESP32",
  "edgeDeviceId": "ESP32-001",
  "description": "Test device"
}
```
5. Click **"Execute"**
6. **Note the `deviceId`** from the response (e.g., `1`)

### Step 2: Create a Sensor
1. Find **"Sensors"** section
2. Click on **`POST /api/v1/Sensors/devices/{deviceId}/sensors`**
3. Click **"Try it out"**
4. Enter the `deviceId` from Step 1 (e.g., `1`)
5. Replace the JSON with:
```json
{
  "sensorName": "Temperature Sensor",
  "sensorType": "Temperature",
  "unit": "°C",
  "minValue": -40,
  "maxValue": 85
}
```
6. Click **"Execute"**
7. **Note the `sensorId`** from the response (e.g., `1`)

### Step 3: Create an Alert Rule
1. Find **"AlertRules"** section
2. Click on **`POST /api/v1/AlertRules`**
3. Click **"Try it out"**
4. Replace the JSON with (use your deviceId and sensorId):
```json
{
  "deviceId": 1,
  "sensorId": 1,
  "ruleName": "High Temperature Alert",
  "ruleType": "threshold",
  "condition": "Temperature exceeds 30 degrees",
  "thresholdValue": 30.0,
  "comparisonOperator": ">",
  "severity": "High",
  "isEnabled": true
}
```
5. Click **"Execute"**

### Step 4: Create a Sensor Reading (Normal)
1. Find **"SensorReadings"** section
2. Click on **`POST /api/v1/SensorReadings`**
3. Click **"Try it out"**
4. Replace the JSON with:
```json
{
  "deviceId": 1,
  "sensorId": 1,
  "value": 25.0,
  "timestamp": "2025-12-20T10:30:00Z",
  "status": "Good",
  "quality": "High"
}
```
5. Click **"Execute"**
   - This should NOT trigger an alert (25.0 < 30.0)

### Step 5: Create a Sensor Reading (High - Triggers Alert)
1. Same endpoint: **`POST /api/v1/SensorReadings`**
2. Click **"Try it out"** again
3. Replace the JSON with:
```json
{
  "deviceId": 1,
  "sensorId": 1,
  "value": 35.5,
  "timestamp": "2025-12-20T10:31:00Z",
  "status": "Good",
  "quality": "High"
}
```
4. Click **"Execute"**
   - This SHOULD trigger an alert (35.5 > 30.0)

### Step 6: Check for Alerts
1. Find **"Alerts"** section
2. Click on **`GET /api/v1/Alerts/active`**
3. Click **"Try it out"**
4. Click **"Execute"**
5. You should see the alert that was triggered!

### Step 7: Acknowledge Alert
1. Find **"Alerts"** section
2. Click on **`PUT /api/v1/Alerts/{id}/acknowledge`**
3. Click **"Try it out"**
4. Enter the `alertId` from Step 6
5. Click **"Execute"**

### Step 8: Resolve Alert
1. Find **"Alerts"** section
2. Click on **`PUT /api/v1/Alerts/{id}/resolve`**
3. Click **"Try it out"**
4. Enter the same `alertId`
5. Click **"Execute"**

## Understanding Response Codes

- **200 OK** - Request succeeded
- **201 Created** - Resource created successfully
- **204 No Content** - Success with no content
- **400 Bad Request** - Invalid request data
- **404 Not Found** - Resource doesn't exist
- **500 Internal Server Error** - Server error

## Tips

1. **Copy IDs**: When you create resources, copy the IDs for use in other requests
2. **Use "Try it out"**: This button enables editing and testing
3. **Check Response**: Always scroll down to see the full response
4. **Error Messages**: If you get an error, check the response body for details
5. **Collapse Sections**: Click the section headers to collapse/expand them

## Testing SignalR

To test real-time updates:
1. Open `test-signalr.html` in your browser
2. Update the URL to: `http://localhost:5286/monitoringhub`
3. Connect and subscribe to devices/alerts
4. Create sensor readings in Swagger
5. Watch real-time notifications appear!

## Common Issues

### "No devices found"
- Create a device first using `POST /api/v1/Devices`

### "Device not found"
- Check that you're using the correct `deviceId`
- List all devices first: `GET /api/v1/Devices`

### "Alert not triggered"
- Verify the alert rule is enabled (`isEnabled: true`)
- Check the threshold value and comparison operator
- Ensure the sensor reading value actually exceeds the threshold

---

**Happy Testing!** 🎉

