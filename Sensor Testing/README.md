# Sensor testing

| Asset | Purpose |
|--------|---------|
| `simulator-local.py` | Fake temperature → local MQTT (`localhost:1883`) |
| `simulator-cloud.py` | Fake temperature → HiveMQ cloud (TLS) |
| `uno_r4_mqtt_local/` | **Arduino Uno R4 WiFi** → local Mosquitto (DHT11 + onboard temp) |
| `uno_r4_mqtt_cloud/` | **Arduino Uno R4 WiFi** → HiveMQ cloud (TLS) |

## Suggested sensor IDs in the database

| Source | Typical IDs |
|--------|-------------|
| Local simulator | sensor **1** |
| Cloud simulator | sensor **2** |
| Arduino (real hardware) | temp **3**, humidity **4** on device **1** |

Edit `DEVICE_ID` / `SENSOR_TEMP_ID` / `SENSOR_HUM_ID` at the top of each sketch to match your UI.

## Arduino Uno R4 WiFi sketches

**Hardware:** DHT11 on **pin 2** (temperature and humidity).

**Libraries (Arduino Library Manager):**

- ArduinoMqttClient
- DHT sensor library (Adafruit; install Unified Sensor if prompted)

**Board:** Tools → Board → **Arduino Uno R4 WiFi**

### Local (`uno_r4_mqtt_local`)

1. Edit Wi‑Fi credentials and `MQTT_BROKER` (your PC’s IPv4 from `ipconfig`, not `localhost`).
2. Create sensors **3** (temperature) and **4** (humidity) on device **1** in the local UI.
3. Run Mosquitto (`:1883`) and the backend API on the PC.
4. Open `uno_r4_mqtt_local/uno_r4_mqtt_local.ino`, upload, use Serial Monitor at **9600** to verify `pub OK`.

### Cloud (`uno_r4_mqtt_cloud`)

1. Edit Wi‑Fi, `MQTT_PASS` (same as `simulator-cloud.py` / HiveMQ console).
2. Create matching sensor rows in the **cloud** database.
3. Deployed API must subscribe to the same HiveMQ cluster.
4. Upload `uno_r4_mqtt_cloud/uno_r4_mqtt_cloud.ino`.

## Python simulators (optional fakes)

```powershell
cd "Sensor Testing"
python simulator-local.py
python simulator-cloud.py
```

## MQTT topic (all publishers)

`devices/{deviceId}/sensors/{sensorId}/readings`  
Payload: `{"value": 22.5}`
