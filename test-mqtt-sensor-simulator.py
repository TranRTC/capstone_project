#!/usr/bin/env python3
"""
MQTT Sensor Simulator
Simulates IoT sensors sending data to MQTT broker
"""

import paho.mqtt.client as mqtt
import json
import time
import random
import sys
import argparse
from datetime import datetime

# MQTT Broker Configuration
MQTT_BROKER_HOST = "localhost"
MQTT_BROKER_PORT = 1883

def create_sensor_client(sensor_id, device_id):
    """Create and configure MQTT client for a sensor"""
    client_id = f"sensor_{device_id}_{sensor_id}"
    client = mqtt.Client(client_id=client_id)
    
    def on_connect(client, userdata, flags, rc):
        if rc == 0:
            print(f"✓ Sensor {sensor_id} (Device {device_id}) connected to MQTT broker")
        else:
            print(f"✗ Sensor {sensor_id} failed to connect. Return code: {rc}")
    
    def on_publish(client, userdata, mid):
        print(f"  → Message published by sensor {sensor_id} (mid: {mid})")
    
    client.on_connect = on_connect
    client.on_publish = on_publish
    
    return client

def send_sensor_reading(client, device_id, sensor_id, value, topic_format="devices"):
    """Send a sensor reading to MQTT broker"""
    # Create payload
    payload = {
        "value": value,
        "timestamp": datetime.utcnow().isoformat()
    }
    
    # Determine topic based on format
    if topic_format == "devices":
        topic = f"devices/{device_id}/sensors/{sensor_id}/readings"
    elif topic_format == "sensor":
        topic = f"sensor/reading/{device_id}/{sensor_id}"
    else:
        topic = topic_format
    
    # Publish message
    result = client.publish(topic, json.dumps(payload), qos=1)
    
    if result.rc == mqtt.MQTT_ERR_SUCCESS:
        print(f"  📤 Published to {topic}: value={value}")
        return True
    else:
        print(f"  ✗ Failed to publish to {topic}")
        return False

def simulate_sensor(device_id, sensor_id, interval=5, count=10, topic_format="devices"):
    """Simulate a sensor sending readings at regular intervals"""
    print(f"\n{'='*60}")
    print(f"Starting Sensor Simulator")
    print(f"Device ID: {device_id}, Sensor ID: {sensor_id}")
    print(f"Interval: {interval} seconds, Count: {count} readings")
    print(f"Topic Format: {topic_format}")
    print(f"{'='*60}\n")
    
    # Create and connect client
    client = create_sensor_client(sensor_id, device_id)
    
    try:
        print(f"Connecting to MQTT broker at {MQTT_BROKER_HOST}:{MQTT_BROKER_PORT}...")
        client.connect(MQTT_BROKER_HOST, MQTT_BROKER_PORT, 60)
        client.loop_start()
        
        # Wait for connection
        time.sleep(2)
        
        # Send readings
        for i in range(count):
            # Generate random sensor value (0-100)
            value = round(random.uniform(0, 100), 2)
            
            # Send reading
            send_sensor_reading(client, device_id, sensor_id, value, topic_format)
            
            if i < count - 1:
                time.sleep(interval)
        
        # Wait for all messages to be published
        time.sleep(2)
        
        print(f"\n✓ Sent {count} readings from sensor {sensor_id}")
        
    except Exception as e:
        print(f"✗ Error: {e}")
        return False
    finally:
        client.loop_stop()
        client.disconnect()
        print("Disconnected from MQTT broker")
    
    return True

def send_single_reading(device_id, sensor_id, value, topic_format="devices"):
    """Send a single sensor reading"""
    client = create_sensor_client(sensor_id, device_id)
    
    try:
        client.connect(MQTT_BROKER_HOST, MQTT_BROKER_PORT, 60)
        client.loop_start()
        time.sleep(1)
        
        send_sensor_reading(client, device_id, sensor_id, value, topic_format)
        
        time.sleep(1)
        client.loop_stop()
        client.disconnect()
        return True
    except Exception as e:
        print(f"✗ Error: {e}")
        return False

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="MQTT Sensor Simulator")
    parser.add_argument("--device-id", type=int, default=1, help="Device ID")
    parser.add_argument("--sensor-id", type=int, default=1, help="Sensor ID")
    parser.add_argument("--value", type=float, help="Single value to send (if not provided, random values)")
    parser.add_argument("--interval", type=int, default=5, help="Interval between readings (seconds)")
    parser.add_argument("--count", type=int, default=10, help="Number of readings to send")
    parser.add_argument("--topic-format", choices=["devices", "sensor"], default="devices",
                       help="Topic format: 'devices' or 'sensor'")
    
    args = parser.parse_args()
    
    if args.value is not None:
        # Send single reading
        print("Sending single sensor reading...")
        send_single_reading(args.device_id, args.sensor_id, args.value, args.topic_format)
    else:
        # Simulate continuous readings
        simulate_sensor(args.device_id, args.sensor_id, args.interval, args.count, args.topic_format)

