/*
 * Arduino Uno R4 WiFi — CLOUD HiveMQ (TLS :8883)
 *
 * - DHT11 on pin 2: temperature → SENSOR_TEMP_ID, humidity → SENSOR_HUM_ID
 *
 * Topic:  devices/{deviceId}/sensors/{sensorId}/readings
 * Payload: {"value": <float>}
 *
 * BEFORE UPLOAD:
 * 1. DEVICE_ID / SENSOR_* must exist in the CLOUD dashboard DB (not local).
 * 2. MQTT_PASS must match simulator-cloud.py / HiveMQ console.
 * 3. Azure API must have Mqtt:Host, Mqtt:Port=8883, Mqtt:EnableTls=true, credentials set.
 */

#include <WiFiS3.h>
#include <ArduinoMqttClient.h>
#include <DHT.h>

#define DHT_PIN 2
DHT dht(DHT_PIN, DHT11);

// ===================== WIFI (Arduino only — not in simulator) =====================
const char* WIFI_SSID     = "trandiep";
const char* WIFI_PASSWORD = "bingchilling@3614";

// ===================== MQTT — copied from simulator-cloud.py =====================
// MQTT_HOST, MQTT_PORT, MQTT_USERNAME, MQTT_PASSWORD (lines 48-51)
const char* MQTT_BROKER   = "d3221e515d824a45849fcffe802de489.s1.eu.hivemq.cloud";
const int   MQTT_PORT     = 8883;
const char* MQTT_USER     = "iotuser";
const char* MQTT_PASS     = "IoTCapstone2026!";

// Topic pattern (simulator line 82): devices/{DEVICE_ID}/sensors/{SENSOR_ID}/readings
// Payload (simulator lines 83-86): {"value": <float>} — backend accepts value; timestamp optional

// ===================== DEVICE / SENSORS — from simulator-cloud.py =====================
// DEVICE_ID, SENSOR_ID (lines 33-34). Simulator uses one sensor; Arduino adds humidity.
const int DEVICE_ID       = 1;   // simulator-cloud.py DEVICE_ID
const int SENSOR_TEMP_ID  = 3;   // simulator-cloud.py SENSOR_ID (temperature)
const int SENSOR_HUM_ID   = 4;   // second sensor in cloud DB (create in UI if missing)

const unsigned long PUBLISH_MS = 1000;  // simulator default INTERVAL_SECONDS = 5
// =======================================================

WiFiSSLClient wifiClient;
MqttClient mqttClient(wifiClient);

char topicTemp[64];
char topicHum[64];
unsigned long lastPublish = 0;

const unsigned long DHCP_TIMEOUT_MS = 20000;

bool hasValidIp() {
  return WiFi.localIP()[0] != 0;
}

bool waitForDhcp() {
  Serial.print(" waiting for IP");
  unsigned long start = millis();
  while (!hasValidIp() && millis() - start < DHCP_TIMEOUT_MS) {
    delay(500);
    Serial.print("+");
  }
  Serial.println();
  return hasValidIp();
}

bool wifiReady() {
  return WiFi.status() == WL_CONNECTED && hasValidIp();
}

void buildTopics() {
  snprintf(topicTemp, sizeof(topicTemp),
           "devices/%d/sensors/%d/readings", DEVICE_ID, SENSOR_TEMP_ID);
  snprintf(topicHum, sizeof(topicHum),
           "devices/%d/sensors/%d/readings", DEVICE_ID, SENSOR_HUM_ID);
}

void connectWiFi() {
  Serial.print("WiFi");
  if (WiFi.status() == WL_NO_MODULE) {
    Serial.println(" - no WiFi module!");
    for (;;) {
      delay(1000);
    }
  }

  while (!wifiReady()) {
    WiFi.disconnect();
    delay(500);

    Serial.print(" connect");
    while (WiFi.begin(WIFI_SSID, WIFI_PASSWORD) != WL_CONNECTED) {
      Serial.print(".");
      delay(500);
    }
    Serial.println(" linked");

    if (!waitForDhcp()) {
      Serial.println("No IP (0.0.0.0) — check SSID/password, 2.4 GHz Wi-Fi");
      delay(3000);
    }
  }

  Serial.print("Board IP: ");
  Serial.println(WiFi.localIP());
}

bool testTlsToBroker() {
  Serial.print("TLS probe ");
  WiFiSSLClient probe;
  if (probe.connect(MQTT_BROKER, MQTT_PORT)) {
    Serial.println("OK");
    probe.stop();
    return true;
  }
  Serial.println("FAIL — check Wi-Fi / port 8883");
  return false;
}

void connectMqtt() {
  // simulator client_id pattern: sensor_{DEVICE_ID}_{SENSOR_ID}
  mqttClient.setId("uno_r4_cloud_1");
  mqttClient.setUsernamePassword(MQTT_USER, MQTT_PASS);

  Serial.print("MQTTS -> ");
  Serial.println(MQTT_BROKER);

  while (true) {
    if (!testTlsToBroker()) {
      delay(5000);
      continue;
    }
    if (mqttClient.connect(MQTT_BROKER, MQTT_PORT)) {
      break;
    }
    Serial.print("  MQTT retry, err=");
    Serial.println(mqttClient.connectError());
    delay(3000);
  }
  Serial.println("MQTT connected (TLS)");
  Serial.println(topicTemp);
  Serial.println(topicHum);
}

bool publishReading(const char* topic, float value) {
  char payload[48];
  snprintf(payload, sizeof(payload), "{\"value\":%.2f}", value);
  Serial.print("  -> ");
  Serial.print(topic);
  Serial.print(" ");
  Serial.println(payload);
  mqttClient.beginMessage(topic, false, 1);
  mqttClient.print(payload);
  bool ok = mqttClient.endMessage() != 0;
  mqttClient.poll();
  return ok;
}

void setup() {
  Serial.begin(9600);
  delay(2000);
  dht.begin();
  buildTopics();
  connectWiFi();
  connectMqtt();
}

void loop() {
  if (!wifiReady()) {
    connectWiFi();
  }
  if (wifiReady() && !mqttClient.connected()) {
    connectMqtt();
  }
  mqttClient.poll();

  if (!wifiReady() || !mqttClient.connected()) {
    return;
  }

  if (millis() - lastPublish < PUBLISH_MS) {
    return;
  }
  lastPublish = millis();

  float tempC = dht.readTemperature();
  float hum = dht.readHumidity();

  if (isnan(tempC) || isnan(hum)) {
    Serial.println("DHT11 read failed — check pin 2 / wiring");
    return;
  }

  Serial.print("Temp ");
  Serial.print(tempC);
  Serial.println(publishReading(topicTemp, tempC) ? " pub OK" : " pub FAIL");

  Serial.print("Hum ");
  Serial.print(hum);
  Serial.println(publishReading(topicHum, hum) ? " pub OK" : " pub FAIL");
}
