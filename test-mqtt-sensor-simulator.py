#!/usr/bin/env python3
"""
MQTT Temperature Sensor Simulator
Targets existing Temperature Sensor 1 by default.
Supports TLS and username/password for cloud brokers like HiveMQ.
"""

import paho.mqtt.client as mqtt
import json
import time
import random
import sys
import ssl
import argparse
from datetime import datetime

# MQTT Broker Configuration (overridable via CLI args)
MQTT_BROKER_HOST = "d3221e515d824a45849fcffe802de489.s1.eu.hivemq.cloud"
MQTT_BROKER_PORT = 8883
MQTT_USE_TLS = True
MQTT_USERNAME = "iotuser"
MQTT_PASSWORD = "IoTCapstone2026!"

# Existing default records in this project
DEFAULT_DEVICE_ID = 1
DEFAULT_SENSOR_ID = 1
DEFAULT_SENSOR_LABEL = "Temperature Sensor 1"

def create_sensor_client(sensor_id, device_id):
    """Create and configure MQTT client for a sensor"""
    client_id = f"sensor_{device_id}_{sensor_id}"
    client = mqtt.Client(client_id=client_id)

    if MQTT_USERNAME and MQTT_PASSWORD:
        client.username_pw_set(MQTT_USERNAME, MQTT_PASSWORD)

    if MQTT_USE_TLS:
        client.tls_set(cert_reqs=ssl.CERT_REQUIRED, tls_version=ssl.PROTOCOL_TLS)

    def on_connect(client, userdata, flags, rc):
        if rc == 0:
            print(f"[OK] Sensor {sensor_id} (Device {device_id}) connected to MQTT broker")
        else:
            print(f"[ERR] Sensor {sensor_id} failed to connect. Return code: {rc}")

    def on_publish(client, userdata, mid):
        print(f"  [ACK] Message published by sensor {sensor_id} (mid: {mid})")

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
        print(f"  [PUB] Published to {topic}: value={value}")
        return True
    else:
        print(f"  [ERR] Failed to publish to {topic}")
        return False

def generate_temperature_value():
    """Generate realistic temperature values in Celsius."""
    return round(random.uniform(20.0, 30.0), 2)


def simulate_sensor(device_id, sensor_id, interval=5, count=10, topic_format="devices"):
    """Simulate a temperature sensor sending readings at regular intervals"""
    print(f"\n{'='*60}")
    print(f"Starting Sensor Simulator")
    print(f"Sensor Target: {DEFAULT_SENSOR_LABEL}")
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
            # Generate realistic temperature value
            value = generate_temperature_value()
            
            # Send reading
            send_sensor_reading(client, device_id, sensor_id, value, topic_format)
            
            if i < count - 1:
                time.sleep(interval)
        
        # Wait for all messages to be published
        time.sleep(2)
        
        print(f"\n[OK] Sent {count} readings from sensor {sensor_id}")
        
    except Exception as e:
        print(f"[ERR] Error: {e}")
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
        print(f"[ERR] Error: {e}")
        return False

if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="MQTT Temperature Sensor Simulator (defaults to Temperature Sensor 1)"
    )
    parser.add_argument("--host", type=str, default=MQTT_BROKER_HOST, help="MQTT broker host")
    parser.add_argument("--port", type=int, default=MQTT_BROKER_PORT, help="MQTT broker port")
    parser.add_argument("--no-tls", action="store_true", help="Disable TLS (for local broker)")
    parser.add_argument("--username", type=str, default=MQTT_USERNAME, help="MQTT username")
    parser.add_argument("--password", type=str, default=MQTT_PASSWORD, help="MQTT password")
    parser.add_argument(
        "--device-id",
        type=int,
        default=DEFAULT_DEVICE_ID,
        help=f"Device ID (default: {DEFAULT_DEVICE_ID})"
    )
    parser.add_argument(
        "--sensor-id",
        type=int,
        default=DEFAULT_SENSOR_ID,
        help=f"Sensor ID (default: {DEFAULT_SENSOR_ID})"
    )
    parser.add_argument("--value", type=float, help="Single value to send (if not provided, random values)")
    parser.add_argument("--interval", type=int, default=5, help="Interval between readings (seconds)")
    parser.add_argument("--count", type=int, default=10, help="Number of readings to send")
    parser.add_argument(
        "--topic-format",
        choices=["devices", "sensor"],
        default="devices",
        help="Topic format: 'devices' or 'sensor' (use 'devices' for backend ingestion)"
    )

    args = parser.parse_args()

    # Apply broker settings globally
    MQTT_BROKER_HOST = args.host
    MQTT_BROKER_PORT = args.port
    MQTT_USE_TLS = not args.no_tls
    MQTT_USERNAME = args.username
    MQTT_PASSWORD = args.password

    if args.value is not None:
        print("Sending single sensor reading...")
        send_single_reading(args.device_id, args.sensor_id, args.value, args.topic_format)
    else:
        simulate_sensor(args.device_id, args.sensor_id, args.interval, args.count, args.topic_format)


