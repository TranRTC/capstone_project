#!/usr/bin/env python3
"""
simulator-local.py — Local Temperature Sensor Simulator
========================================================
For LOCAL testing only. Connects to:
  - Local API:          http://localhost:5000/api/v1
  - Local MQTT broker:  localhost:1883 (no TLS, no credentials)

Edit the SENSOR MAPPING section below to match your device/sensor, then run:
  python simulator-local.py

Requires:
  - Backend running at localhost:5000
  - Mosquitto MQTT broker running at localhost:1883
"""

import paho.mqtt.client as mqtt
import json
import time
import random
import sys
import argparse
import requests
from datetime import datetime
from typing import Optional, Tuple

# =============================================================================
# SENSOR MAPPING — Edit these to match your device and sensor in the system
# =============================================================================
DEVICE_ID = 1          # The DeviceId in the database
SENSOR_ID = 3          # The SensorId in the database
SENSOR_LABEL = "Temperature Sensor"   # Label for display only

# =============================================================================
# MQTT BROKER — Local Mosquitto (no TLS, no credentials)
# =============================================================================
MQTT_BROKER_HOST = "localhost"
MQTT_BROKER_PORT = 1883

# =============================================================================
# API
# =============================================================================
API_BASE_URL = "http://localhost:5000/api/v1"

# Default device and sensor configuration (used only if no DEVICE_ID/SENSOR_ID set)
DEFAULT_DEVICE_NAME = "Temperature Monitoring Device"
DEFAULT_DEVICE_TYPE = "Environmental Monitor"
DEFAULT_SENSOR_NAME = "Temperature Sensor"
DEFAULT_SENSOR_TYPE = "Temperature"
DEFAULT_UNIT = "°C"

class TemperatureSimulator:
    def __init__(self, api_url: str = API_BASE_URL, mqtt_host: str = MQTT_BROKER_HOST, mqtt_port: int = MQTT_BROKER_PORT):
        self.api_url = api_url
        self.mqtt_host = mqtt_host
        self.mqtt_port = mqtt_port
        self.device_id: Optional[int] = None
        self.sensor_id: Optional[int] = None
        self.mqtt_client: Optional[mqtt.Client] = None
        self.current_temp = 22.0  # Starting temperature in Celsius
        self.target_temp = 22.0   # Target temperature for gradual changes
        
    def check_api_health(self) -> bool:
        """Check if the API is accessible"""
        try:
            response = requests.get(f"{self.api_url}/health", timeout=5)
            return response.status_code == 200
        except Exception as e:
            print(f"✗ API health check failed: {e}")
            return False
    
    def find_or_create_device(self, device_name: str, device_type: str, location: str = "Simulation Lab") -> Optional[int]:
        """Find existing device or create a new one"""
        try:
            # Try to get existing devices
            response = requests.get(f"{self.api_url}/devices", timeout=5)
            if response.status_code == 200:
                data = response.json()
                devices = data.get('data', []) if isinstance(data, dict) else []
                
                # Look for device with matching name
                for device in devices:
                    if device.get('deviceName') == device_name:
                        device_id = device.get('deviceId')
                        print(f"✓ Found existing device: {device_name} (ID: {device_id})")
                        return device_id
            
            # Create new device
            print(f"Creating new device: {device_name}...")
            device_data = {
                "deviceName": device_name,
                "deviceType": device_type,
                "location": location,
                "description": "Simulated temperature monitoring device"
            }
            
            response = requests.post(
                f"{self.api_url}/devices",
                json=device_data,
                headers={"Content-Type": "application/json"},
                timeout=5
            )
            
            if response.status_code in [200, 201]:
                data = response.json()
                device = data.get('data', {}) if isinstance(data, dict) else data
                device_id = device.get('deviceId')
                print(f"✓ Created device: {device_name} (ID: {device_id})")
                return device_id
            else:
                print(f"✗ Failed to create device: {response.status_code} - {response.text}")
                return None
                
        except Exception as e:
            print(f"✗ Error finding/creating device: {e}")
            return None
    
    def find_or_create_sensor(self, device_id: int, sensor_name: str, sensor_type: str, unit: str = "°C") -> Optional[int]:
        """Find existing temperature sensor or create a new one"""
        try:
            # Try to get existing sensors for this device
            response = requests.get(f"{self.api_url}/sensors/devices/{device_id}/sensors", timeout=5)
            if response.status_code == 200:
                data = response.json()
                sensors = data.get('data', []) if isinstance(data, dict) else []
                
                # Look for temperature sensor
                for sensor in sensors:
                    sensor_type_lower = sensor.get('sensorType', '').lower()
                    if 'temperature' in sensor_type_lower or 'temp' in sensor_type_lower:
                        sensor_id = sensor.get('sensorId')
                        print(f"✓ Found existing temperature sensor: {sensor.get('sensorName')} (ID: {sensor_id})")
                        return sensor_id
            
            # Create new temperature sensor
            print(f"Creating new temperature sensor for device {device_id}...")
            sensor_data = {
                "sensorName": sensor_name,
                "sensorType": sensor_type,
                "unit": unit,
                "minValue": -10.0,
                "maxValue": 50.0
            }
            
            response = requests.post(
                f"{self.api_url}/sensors/devices/{device_id}/sensors",
                json=sensor_data,
                headers={"Content-Type": "application/json"},
                timeout=5
            )
            
            if response.status_code in [200, 201]:
                data = response.json()
                sensor = data.get('data', {}) if isinstance(data, dict) else data
                sensor_id = sensor.get('sensorId')
                print(f"✓ Created sensor: {sensor_name} (ID: {sensor_id})")
                return sensor_id
            else:
                print(f"✗ Failed to create sensor: {response.status_code} - {response.text}")
                return None
                
        except Exception as e:
            print(f"✗ Error finding/creating sensor: {e}")
            return None
    
    def generate_temperature(self) -> float:
        """Generate realistic temperature reading with gradual changes"""
        # Occasionally set a new target temperature (simulating environmental changes)
        if random.random() < 0.1:  # 10% chance to change target
            self.target_temp = round(random.uniform(18.0, 28.0), 1)
        
        # Gradually move current temperature towards target
        diff = self.target_temp - self.current_temp
        if abs(diff) > 0.1:
            # Move 20% of the way towards target each time
            self.current_temp += diff * 0.2
        else:
            # Add small random variations when near target
            self.current_temp += random.uniform(-0.2, 0.2)
        
        # Add some noise
        noise = random.uniform(-0.1, 0.1)
        final_temp = round(self.current_temp + noise, 2)
        
        # Clamp to reasonable range
        final_temp = max(15.0, min(30.0, final_temp))
        self.current_temp = final_temp
        
        return final_temp
    
    def create_mqtt_client(self) -> mqtt.Client:
        """Create and configure MQTT client"""
        client_id = f"temp_sensor_{self.device_id}_{self.sensor_id}"
        client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION1, client_id=client_id)

        def on_connect(client, userdata, flags, rc):
            if rc == 0:
                print(f"✓ Connected to MQTT broker at {self.mqtt_host}:{self.mqtt_port}")
            else:
                print(f"✗ Failed to connect. Return code: {rc}")

        def on_publish(client, userdata, mid):
            pass

        client.on_connect = on_connect
        client.on_publish = on_publish

        return client
    
    def send_temperature_reading(self, temperature: float) -> bool:
        """Send temperature reading via MQTT"""
        if not self.device_id or not self.sensor_id:
            print("✗ Device or sensor ID not set")
            return False

        topic = f"devices/{self.device_id}/sensors/{self.sensor_id}/readings"
        payload = {
            "value": temperature,
            "timestamp": datetime.utcnow().isoformat()
        }

        # Wait up to 5s for connection before publishing
        for _ in range(10):
            if self.mqtt_client.is_connected():
                break
            time.sleep(0.5)

        try:
            result = self.mqtt_client.publish(topic, json.dumps(payload), qos=1)
            if result.rc == mqtt.MQTT_ERR_SUCCESS:
                print(f"  📤 Temperature: {temperature}°C (Device {self.device_id}, Sensor {self.sensor_id})")
                return True
            else:
                print(f"  ✗ Failed to publish temperature reading")
                return False
        except Exception as e:
            print(f"  ✗ Error publishing: {e}")
            return False
    
    def run(self, 
            device_name: str = DEFAULT_DEVICE_NAME,
            device_type: str = DEFAULT_DEVICE_TYPE,
            sensor_name: str = DEFAULT_SENSOR_NAME,
            sensor_type: str = DEFAULT_SENSOR_TYPE,
            location: str = "Simulation Lab",
            interval: int = 5,
            duration: Optional[int] = None,
            device_id: Optional[int] = None,
            sensor_id: Optional[int] = None):
        """Run the temperature sensor simulator"""
        print(f"\n{'='*70}")
        print(f"Temperature Sensor Simulator")
        print(f"{'='*70}\n")
        
        # Check API health
        print("Checking API connection...")
        if not self.check_api_health():
            print("✗ Cannot connect to API. Make sure the backend is running at http://localhost:5000")
            return False
        print("✓ API is accessible\n")
        
        if device_id and sensor_id:
            # Use hardcoded or CLI-provided IDs directly — skip find/create
            self.device_id = device_id
            self.sensor_id = sensor_id
            print(f"✓ Using Device ID: {self.device_id}, Sensor ID: {self.sensor_id} ({SENSOR_LABEL})\n")
        else:
            # Find or create device
            print("Setting up device...")
            self.device_id = self.find_or_create_device(device_name, device_type, location)
            if not self.device_id:
                print("✗ Failed to setup device")
                return False
            
            # Find or create sensor
            print("\nSetting up sensor...")
            self.sensor_id = self.find_or_create_sensor(self.device_id, sensor_name, sensor_type)
            if not self.sensor_id:
                print("✗ Failed to setup sensor")
                return False
        
        # Connect to MQTT
        print(f"\nConnecting to MQTT broker at {self.mqtt_host}:{self.mqtt_port}...")
        self.mqtt_client = self.create_mqtt_client()
        
        try:
            self.mqtt_client.reconnect_delay_set(min_delay=2, max_delay=10)
            self.mqtt_client.connect(self.mqtt_host, self.mqtt_port, keepalive=60)
            self.mqtt_client.loop_start()

            # Wait for connection to stabilise
            time.sleep(3)
            
            print(f"\n{'='*70}")
            print(f"Starting temperature simulation...")
            print(f"Device ID: {self.device_id}, Sensor ID: {self.sensor_id}")
            print(f"Update interval: {interval} seconds")
            if duration:
                print(f"Duration: {duration} seconds")
            else:
                print(f"Running continuously (Press Ctrl+C to stop)")
            print(f"{'='*70}\n")
            
            start_time = time.time()
            reading_count = 0
            
            try:
                while True:
                    # Generate temperature
                    temperature = self.generate_temperature()
                    
                    # Send reading
                    if self.send_temperature_reading(temperature):
                        reading_count += 1
                    
                    # Check duration
                    if duration and (time.time() - start_time) >= duration:
                        break
                    
                    # Wait for next reading
                    time.sleep(interval)
                    
            except KeyboardInterrupt:
                print(f"\n\nStopping simulator...")
            
            print(f"\n{'='*70}")
            print(f"Simulation complete!")
            print(f"Total readings sent: {reading_count}")
            print(f"{'='*70}\n")
            
        except Exception as e:
            print(f"✗ Error during simulation: {e}")
            return False
        finally:
            if self.mqtt_client:
                self.mqtt_client.loop_stop()
                self.mqtt_client.disconnect()
                print("Disconnected from MQTT broker")
        
        return True


def main():
    parser = argparse.ArgumentParser(
        description="Temperature Sensor Simulator - Creates device/sensor and sends temperature readings via MQTT",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Run with default settings (5 second intervals, continuous)
  python simulator-local.py
  
  # Run for 60 seconds with 3 second intervals
  python simulator-local.py --interval 3 --duration 60
  
  # Use custom device and sensor names
  python simulator-local.py --device-name "Lab Sensor" --sensor-name "Room Temp"
        """
    )
    
    parser.add_argument("--device-name", type=str, default=DEFAULT_DEVICE_NAME,
                       help=f"Device name (default: {DEFAULT_DEVICE_NAME})")
    parser.add_argument("--device-type", type=str, default=DEFAULT_DEVICE_TYPE,
                       help=f"Device type (default: {DEFAULT_DEVICE_TYPE})")
    parser.add_argument("--sensor-name", type=str, default=DEFAULT_SENSOR_NAME,
                       help=f"Sensor name (default: {DEFAULT_SENSOR_NAME})")
    parser.add_argument("--sensor-type", type=str, default=DEFAULT_SENSOR_TYPE,
                       help=f"Sensor type (default: {DEFAULT_SENSOR_TYPE})")
    parser.add_argument("--location", type=str, default="Simulation Lab",
                       help="Device location (default: Simulation Lab)")
    parser.add_argument("--interval", type=int, default=5,
                       help="Interval between readings in seconds (default: 5)")
    parser.add_argument("--duration", type=int, default=None,
                       help="Duration to run in seconds (default: run indefinitely)")
    parser.add_argument("--device-id", type=int, default=DEVICE_ID,
                       help=f"Device ID to simulate (default: {DEVICE_ID})")
    parser.add_argument("--sensor-id", type=int, default=SENSOR_ID,
                       help=f"Sensor ID to simulate (default: {SENSOR_ID})")
    parser.add_argument("--api-url", type=str, default=API_BASE_URL,
                       help=f"API base URL (default: {API_BASE_URL})")
    parser.add_argument("--mqtt-host", type=str, default=MQTT_BROKER_HOST,
                       help=f"MQTT broker host (default: {MQTT_BROKER_HOST})")
    parser.add_argument("--mqtt-port", type=int, default=MQTT_BROKER_PORT,
                       help=f"MQTT broker port (default: {MQTT_BROKER_PORT})")
    
    args = parser.parse_args()
    
    simulator = TemperatureSimulator(
        api_url=args.api_url,
        mqtt_host=args.mqtt_host,
        mqtt_port=args.mqtt_port
    )
    
    success = simulator.run(
        device_name=args.device_name,
        device_type=args.device_type,
        sensor_name=args.sensor_name,
        sensor_type=args.sensor_type,
        location=args.location,
        interval=args.interval,
        duration=args.duration,
        device_id=args.device_id,
        sensor_id=args.sensor_id
    )
    
    sys.exit(0 if success else 1)


if __name__ == "__main__":
    main()

