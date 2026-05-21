#!/usr/bin/env python3
"""
simulator-cloud.py — Cloud MQTT Sensor Simulator
=================================================
Simulates a real physical sensor mapped to a specific Device and Sensor in the system.
Connects directly to HiveMQ cloud broker with TLS.

To use: edit the SENSOR MAPPING section below to match your device/sensor IDs,
then just run:  python simulator-cloud.py

Optional modes (via CLI):
  python simulator-cloud.py                  # continuous random readings
  python simulator-cloud.py --count 20       # send exactly 20 readings then stop
  python simulator-cloud.py --interval 3     # 3-second interval between readings
  python simulator-cloud.py --value 85       # send ONE specific value (for alert testing)

Requires:
  - No local services needed
  - Device and Sensor must already exist in the database
"""

import paho.mqtt.client as mqtt
import json
import time
import random
import ssl
import argparse
from datetime import datetime

# =============================================================================
# SENSOR MAPPING — Edit these to match your device and sensor in the system
# =============================================================================
DEVICE_ID = 1          # The DeviceId in the database
SENSOR_ID = 1          # The SensorId in the database
SENSOR_LABEL = "Temperature Sensor 1"   # Label for display only

# =============================================================================
# READING SETTINGS
# =============================================================================
INTERVAL_SECONDS = 5       # How often to send a reading
COUNT = 0                   # How many readings to send (0 = run continuously)
TEMPERATURE_MIN = 20.0      # Minimum random temperature value
TEMPERATURE_MAX = 30.0      # Maximum random temperature value

# =============================================================================
# MQTT BROKER — HiveMQ Cloud (do not change unless broker changes)
# =============================================================================
MQTT_HOST = "d3221e515d824a45849fcffe802de489.s1.eu.hivemq.cloud"
MQTT_PORT = 8883
MQTT_USERNAME = "iotuser"
MQTT_PASSWORD = "IoTCapstone2026!"

# =============================================================================


def generate_value():
    """Generate a random temperature reading."""
    return round(random.uniform(TEMPERATURE_MIN, TEMPERATURE_MAX), 2)


def on_connect(client, userdata, flags, rc):
    if rc == 0:
        print(f"[OK] Connected to MQTT broker")
    else:
        print(f"[ERR] Failed to connect. Return code: {rc}")


def on_publish(client, userdata, mid):
    pass  # silent ack


def create_client():
    client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION1, client_id=f"sensor_{DEVICE_ID}_{SENSOR_ID}")
    client.username_pw_set(MQTT_USERNAME, MQTT_PASSWORD)
    client.tls_set(cert_reqs=ssl.CERT_REQUIRED, tls_version=ssl.PROTOCOL_TLS)
    client.on_connect = on_connect
    client.on_publish = on_publish
    return client


def publish_reading(client, value):
    topic = f"devices/{DEVICE_ID}/sensors/{SENSOR_ID}/readings"
    payload = {
        "value": value,
        "timestamp": datetime.utcnow().isoformat()
    }
    result = client.publish(topic, json.dumps(payload), qos=1)
    if result.rc == mqtt.MQTT_ERR_SUCCESS:
        print(f"  [PUB] {SENSOR_LABEL} → {value}°C  (Device {DEVICE_ID}, Sensor {SENSOR_ID})")
        return True
    else:
        print(f"  [ERR] Failed to publish reading")
        return False


def run(interval, count, fixed_value=None):
    print(f"\n{'='*60}")
    print(f"  Cloud Sensor Simulator")
    print(f"  Sensor : {SENSOR_LABEL}")
    print(f"  Device ID: {DEVICE_ID}  |  Sensor ID: {SENSOR_ID}")
    if fixed_value is not None:
        print(f"  Mode   : Single value → {fixed_value}")
    elif count > 0:
        print(f"  Mode   : {count} readings, {interval}s interval")
    else:
        print(f"  Mode   : Continuous, {interval}s interval (Ctrl+C to stop)")
    print(f"  Broker : {MQTT_HOST}:{MQTT_PORT}")
    print(f"{'='*60}\n")

    client = create_client()

    try:
        print(f"Connecting to broker...")
        client.connect(MQTT_HOST, MQTT_PORT, 60)
        client.loop_start()
        time.sleep(2)  # wait for connection

        if fixed_value is not None:
            # Single value mode — send once and exit
            publish_reading(client, fixed_value)
            time.sleep(1)
        elif count > 0:
            # Fixed count mode
            for i in range(count):
                publish_reading(client, generate_value())
                if i < count - 1:
                    time.sleep(interval)
            time.sleep(1)
            print(f"\n[OK] Sent {count} readings")
        else:
            # Continuous mode
            reading_count = 0
            try:
                while True:
                    publish_reading(client, generate_value())
                    reading_count += 1
                    time.sleep(interval)
            except KeyboardInterrupt:
                print(f"\n[STOP] Interrupted. Sent {reading_count} readings total.")

    except Exception as e:
        print(f"[ERR] {e}")
    finally:
        client.loop_stop()
        client.disconnect()
        print("Disconnected.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="Cloud sensor simulator — edit SENSOR MAPPING at top of file to configure."
    )
    parser.add_argument("--value", type=float, default=None,
                        help="Send a single specific value then exit (useful for alert testing)")
    parser.add_argument("--interval", type=int, default=INTERVAL_SECONDS,
                        help=f"Seconds between readings (default: {INTERVAL_SECONDS})")
    parser.add_argument("--count", type=int, default=COUNT,
                        help=f"Number of readings to send, 0=continuous (default: {COUNT})")

    args = parser.parse_args()
    run(interval=args.interval, count=args.count, fixed_value=args.value)
