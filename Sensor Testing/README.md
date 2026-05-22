# Sensor testing

| Asset | Purpose |
|--------|---------|
| `simulator-local.py` | Fake temperature → local MQTT (`localhost:1883`) |
| `simulator-cloud.py` | Fake temperature → HiveMQ cloud (TLS) |
| `uno_r4_mqtt_local/` | Arduino → local Mosquitto (DHT11 + digital I/O) |
| `uno_r4_mqtt_cloud/` | Arduino → HiveMQ cloud (TLS) |
| `Check-CloudPipeline.ps1` | Quick Azure MQTT ingest health check |

## Pin map (Uno R4 WiFi)

| Pin | Role | Dashboard mapping |
|-----|------|-------------------|
| **2** | DHT11 data (temp + humidity) | Analog sensors |
| **4** | Digital input 1 (`INPUT_PULLUP`) | **Discrete** sensor, value 0/1 |
| **5** | Digital input 2 | **Discrete** sensor |
| **7** | Digital output 1 | **Discrete** actuator, **Channel** = `7` |
| **8** | Digital output 2 | **Discrete** actuator, **Channel** = `8` |
| **3** | Analog output (PWM) | **Analog** actuator, **Channel** = `3`, min 0 max 100 |

Wiring DI: one side to pin, other to **GND** (pressed = 0, released = 1 with pull-up).  
DO: LED/relay on pin → GND (use series resistor for LED).

## Suggested IDs (edit in each `.ino` to match your UI)

### Local (`uno_r4_mqtt_local.ino`)

| Item | Default ID |
|------|------------|
| Device | 1026 |
| Temp / Hum (DHT11) | 15 / 16 |
| Digital in 1 / 2 | 17 / 18 |

### Cloud (`uno_r4_mqtt_cloud.ino`)

| Item | Default ID |
|------|------------|
| Device | 1 |
| Temp / Hum | 1 / 2 |
| Digital in 1 / 2 | 5 / 6 |

## Dashboard setup (local and cloud)

### Digital inputs (readings)

1. **Sensors** → Add sensor.
2. **Signal kind:** Discrete.
3. Sensor IDs must match `SENSOR_DI1_ID` / `SENSOR_DI2_ID` in the sketch.
4. View in **Live** mode (0/1 indicator).

### Digital outputs (on/off)

1. **Actuators** → Add actuator.
2. **Kind:** Discrete.
3. **Channel:** `7` or `8` (must match GPIO pin).
4. **Control** → toggle → sends `SetPower` with `{"on": true/false}`.

### Analog output (PWM)

1. **Actuators** → Add actuator.
2. **Kind:** Analog.
3. **Channel:** `3`.
4. **Analog min / max:** `0` and `100` (matches sketch mapping to PWM).
5. **Control** → slider → sends `SetValue` with `{"value": 50}`.

Commands use MQTT topic `devices/{deviceId}/commands` (no board IP). The sketch publishes ACK on `devices/{deviceId}/commands/ack`.

## Libraries

- ArduinoMqttClient
- DHT sensor library
- Board: **Arduino Uno R4 WiFi**

## Local test

1. Mosquitto `:1883`, API running, sensors/actuators in **local** DB.
2. Upload `uno_r4_mqtt_local/uno_r4_mqtt_local.ino`.
3. Serial Monitor 9600: `Subscribe devices/.../commands`, `DI`, `CMD` when you click Control.

## Cloud test

1. Matching rows in **cloud** DB; HiveMQ credentials in sketch.
2. Upload `uno_r4_mqtt_cloud/uno_r4_mqtt_cloud.ino`.
3. `Check-CloudPipeline.ps1` or `/api/v1/health/mqtt` — `totalMessagesReceived` should increase.

## MQTT topics

| Direction | Topic |
|-----------|--------|
| Board → cloud/local | `devices/{deviceId}/sensors/{sensorId}/readings` |
| Cloud/local → board | `devices/{deviceId}/commands` |
| Board → cloud/local | `devices/{deviceId}/commands/ack` |

Payload reading: `{"value": 22.5}` or `{"value": 1}` for discrete.
