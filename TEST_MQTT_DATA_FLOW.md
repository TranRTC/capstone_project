# MQTT Data Flow Testing Guide

This guide explains how to test the data flow from sensors → MQTT broker → server.

## Architecture Overview

```
Sensor → MQTT Broker → Server (Backend API) → Database
```

1. **Sensor**: Simulated IoT device that publishes sensor readings to MQTT topics
2. **MQTT Broker**: Message broker (e.g., Mosquitto) that routes messages
3. **Server**: ASP.NET Core API with MQTT service that subscribes to topics and saves data

## Prerequisites

### 1. MQTT Broker
You need an MQTT broker running on `localhost:1883`. 

**Option A: Mosquitto (Recommended)**
```powershell
# Install via Chocolatey
choco install mosquitto

# Or download from: https://mosquitto.org/download/

# Start the service
net start mosquitto
```

**Option B: Docker**
```powershell
docker run -it -p 1883:1883 eclipse-mosquitto
```

### 2. Backend API
The backend API must be running with the MQTT service enabled.

```powershell
cd Backend/IoTMonitoringSystem.API
dotnet run
```

The MQTT service will automatically:
- Connect to the broker at `localhost:1883`
- Subscribe to topics: `devices/+/sensors/+/readings` and `sensor/reading/+/+`
- Process incoming messages and save them to the database

### 3. Python (for sensor simulator)
```powershell
# Install Python (if not already installed)
# Download from: https://www.python.org/downloads/

# Install paho-mqtt library
pip install paho-mqtt
```

## Testing Methods

### Method 1: Automated PowerShell Test (Recommended)

This script performs a complete end-to-end test:

```powershell
.\test-mqtt-data-flow.ps1
```

**What it does:**
1. Checks if MQTT broker is accessible
2. Checks if backend API is running
3. Gets or creates a test device and sensor
4. Sends a test sensor reading via MQTT
5. Verifies the data was received and saved in the database

### Method 2: Python Sensor Simulator

Send single reading:
```powershell
python test-mqtt-sensor-simulator.py --device-id 1 --sensor-id 1 --value 25.5
```

Send multiple readings (simulate continuous sensor):
```powershell
python test-mqtt-sensor-simulator.py --device-id 1 --sensor-id 1 --count 10 --interval 5
```

**Parameters:**
- `--device-id`: Device ID (default: 1)
- `--sensor-id`: Sensor ID (default: 1)
- `--value`: Single value to send (optional, if not provided uses random)
- `--count`: Number of readings to send (default: 10)
- `--interval`: Seconds between readings (default: 5)
- `--topic-format`: Topic format - `devices` or `sensor` (default: `devices`)

### Method 3: Using MQTT Client Tools

**Using mosquitto_pub (command line):**
```powershell
# Install mosquitto tools
choco install mosquitto

# Send a reading
mosquitto_pub -h localhost -p 1883 -t "devices/1/sensors/1/readings" -m '{"value": 25.5}'
```

**Using MQTT.fx (GUI):**
1. Download from: https://mqttfx.jensd.de/
2. Connect to `localhost:1883`
3. Publish to topic: `devices/1/sensors/1/readings`
4. Payload: `{"value": 25.5}`

## MQTT Topic Formats

The server subscribes to two topic patterns:

1. **Device-based format**: `devices/{deviceId}/sensors/{sensorId}/readings`
   - Example: `devices/1/sensors/2/readings`
   - Payload: `{"value": 25.5}`

2. **Sensor-based format**: `sensor/reading/{deviceId}/{sensorId}`
   - Example: `sensor/reading/1/2`
   - Payload: `{"value": 25.5, "deviceId": 1, "sensorId": 2}`

## Message Payload Format

**Required fields:**
- `value`: Decimal number (sensor reading value)

**Optional fields:**
- `timestamp`: ISO 8601 timestamp (if not provided, server uses current time)
- `deviceId`: Device ID (if not in topic)
- `sensorId`: Sensor ID (if not in topic)

**Example payloads:**
```json
{"value": 25.5}
{"value": 25.5, "timestamp": "2024-01-15T10:30:00Z"}
{"value": 25.5, "deviceId": 1, "sensorId": 2}
```

## Verifying Data Reception

### Check via API
```powershell
# Get all readings for a sensor
Invoke-RestMethod -Uri "http://localhost:5000/api/v1/sensorreadings?sensorId=1" -Method Get

# Get latest reading
Invoke-RestMethod -Uri "http://localhost:5000/api/v1/sensorreadings?sensorId=1&pageSize=1" -Method Get
```

### Check Backend Logs
The backend logs will show:
```
Connected to MQTT broker at localhost:1883
MQTT message received on topic: devices/1/sensors/1/readings
```

## Troubleshooting

### MQTT Broker Not Accessible
- Ensure Mosquitto is running: `net start mosquitto`
- Check if port 1883 is open: `Test-NetConnection -ComputerName localhost -Port 1883`
- Verify firewall settings

### Backend Not Receiving Messages
- Check backend logs for MQTT connection errors
- Verify MQTT configuration in `appsettings.json`:
  ```json
  "Mqtt": {
    "Host": "localhost",
    "Port": 1883
  }
  ```
- Ensure the MQTT service started successfully (check startup logs)

### Data Not Saved
- Verify device and sensor exist in database
- Check backend logs for processing errors
- Verify database connection is working

### Python Script Errors
- Ensure Python is installed: `python --version`
- Install paho-mqtt: `pip install paho-mqtt`
- Check Python path is in system PATH

## Expected Behavior

When a sensor reading is sent:
1. ✅ Sensor publishes message to MQTT broker
2. ✅ Broker routes message to subscribed clients
3. ✅ Backend MQTT service receives message
4. ✅ Backend processes and validates data
5. ✅ Data is saved to database
6. ✅ SignalR notification sent to connected clients (if any)
7. ✅ Data appears in API responses

## Testing Checklist

- [ ] MQTT broker is running
- [ ] Backend API is running
- [ ] Device and sensor exist in database
- [ ] Test script can connect to broker
- [ ] Test script can send messages
- [ ] Backend receives messages (check logs)
- [ ] Data is saved to database
- [ ] Data appears in API responses
- [ ] SignalR notifications work (if frontend connected)

