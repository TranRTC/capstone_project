# Temperature Sensor Simulator

This script creates a temperature sensor simulation that connects to your IoT monitoring system backend and sends realistic temperature readings via MQTT. The readings are automatically displayed in real-time on the frontend.

## Prerequisites

1. **Python 3.7+** with the following packages:
   ```bash
   pip install paho-mqtt requests
   ```

2. **Backend API** running at `http://localhost:5000`
   - The API should be running and accessible
   - Database should be set up and migrations applied

3. **MQTT Broker** running at `localhost:1883`
   - Mosquitto or any MQTT broker should be running
   - Default port: 1883

4. **Frontend** running (optional, for viewing real-time updates)
   - The frontend should be running to see the real-time temperature chart
   - Navigate to the device detail page to see the temperature chart

## Usage

### Basic Usage (Default Settings)

Run the simulator with default settings (5-second intervals, continuous):

```bash
python temperature-sensor-simulator.py
```

### Custom Options

```bash
# Run for 60 seconds with 3-second intervals
python temperature-sensor-simulator.py --interval 3 --duration 60

# Use custom device and sensor names
python temperature-sensor-simulator.py --device-name "Lab Sensor" --sensor-name "Room Temperature"

# Custom location
python temperature-sensor-simulator.py --location "Building A - Room 101"

# Custom MQTT broker
python temperature-sensor-simulator.py --mqtt-host "192.168.1.100" --mqtt-port 1883
```

### Command Line Options

- `--device-name`: Name of the device (default: "Temperature Monitoring Device")
- `--device-type`: Type of device (default: "Environmental Monitor")
- `--sensor-name`: Name of the sensor (default: "Temperature Sensor")
- `--sensor-type`: Type of sensor (default: "Temperature")
- `--location`: Device location (default: "Simulation Lab")
- `--interval`: Interval between readings in seconds (default: 5)
- `--duration`: Duration to run in seconds (default: run indefinitely)
- `--api-url`: API base URL (default: http://localhost:5000/api/v1)
- `--mqtt-host`: MQTT broker host (default: localhost)
- `--mqtt-port`: MQTT broker port (default: 1883)

## How It Works

1. **Device & Sensor Setup**: 
   - The script checks if a device with the specified name exists
   - If not found, it creates a new device via the API
   - It then checks for a temperature sensor on that device
   - If no temperature sensor exists, it creates one

2. **Temperature Simulation**:
   - Generates realistic temperature readings (15-30°C)
   - Uses gradual changes instead of random jumps
   - Simulates environmental temperature variations

3. **MQTT Publishing**:
   - Connects to the MQTT broker
   - Publishes readings to topic: `devices/{deviceId}/sensors/{sensorId}/readings`
   - Backend MQTT service receives and processes the readings

4. **Real-Time Updates**:
   - Backend saves readings to database
   - Backend sends SignalR notifications to connected clients
   - Frontend receives updates and displays them in real-time charts

## Viewing Real-Time Updates

1. **Start the simulator** (as described above)

2. **Open the frontend** in your browser (typically `http://localhost:3000`)

3. **Navigate to Devices** page and find your device

4. **Click on the device** to view the device detail page

5. **See the real-time temperature chart** that updates automatically as new readings arrive

## Troubleshooting

### "Cannot connect to API"
- Make sure the backend API is running at `http://localhost:5000`
- Check that the API is accessible: `curl http://localhost:5000/api/v1/health`

### "Failed to connect to MQTT broker"
- Ensure MQTT broker (Mosquitto) is running
- Check MQTT broker is accessible on port 1883
- Verify MQTT configuration in `appsettings.json`

### "No temperature data in frontend"
- Make sure SignalR is connected (check Dashboard for connection status)
- Verify the device has a temperature sensor (check Sensors page)
- Check browser console for any errors
- Ensure you're viewing the correct device detail page

### Temperature readings not appearing
- Check backend logs for MQTT message processing
- Verify the MQTT topic format matches: `devices/{deviceId}/sensors/{sensorId}/readings`
- Check database to see if readings are being saved

## Example Output

```
======================================================================
Temperature Sensor Simulator
======================================================================

Checking API connection...
✓ API is accessible

Setting up device...
✓ Found existing device: Temperature Monitoring Device (ID: 1)

Setting up sensor...
✓ Found existing temperature sensor: Temperature Sensor (ID: 1)

Connecting to MQTT broker at localhost:1883...
✓ Connected to MQTT broker at localhost:1883

======================================================================
Starting temperature simulation...
Device ID: 1, Sensor ID: 1
Update interval: 5 seconds
Running continuously (Press Ctrl+C to stop)
======================================================================

  📤 Temperature: 22.15°C (Device 1, Sensor 1)
  📤 Temperature: 22.23°C (Device 1, Sensor 1)
  📤 Temperature: 22.31°C (Device 1, Sensor 1)
  ...
```

## Notes

- The simulator will reuse existing devices and sensors if they match the names
- Temperature values gradually change to simulate realistic behavior
- Press `Ctrl+C` to stop the simulator
- The simulator handles reconnection automatically if MQTT connection is lost

