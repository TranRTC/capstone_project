/*
 * Arduino Uno R4 WiFi — LOCAL Mosquitto (no TLS)
 *
 * - DHT11 on pin 2: temperature → SENSOR_TEMP_ID, humidity → SENSOR_HUM_ID
 *
 * Topic:  devices/{deviceId}/sensors/{sensorId}/readings
 * Payload: {"value": <float>}
 *
 * Requires: Mosquitto on PC :1883, backend API running, sensors in local DB.
 * MQTT_BROKER = your PC's LAN IPv4 (ipconfig), NOT "localhost".
 */

#include <WiFiS3.h>
#include <ArduinoMqttClient.h>
#include <DHT.h>

#define DHT_PIN 2
DHT dht(DHT_PIN, DHT11);

// ===================== EDIT CONFIG =====================
const char* WIFI_SSID     = "trandiep";
const char* WIFI_PASSWORD = "bingchilling@3614";

// PC Wi-Fi IPv4 from ipconfig — NOT "localhost" on the Arduino
const int MQTT_HOST_OCTETS[] = { 192, 168, 0, 112 };
const int   MQTT_PORT        = 1883;

IPAddress mqttHost(
  MQTT_HOST_OCTETS[0],
  MQTT_HOST_OCTETS[1],
  MQTT_HOST_OCTETS[2],
  MQTT_HOST_OCTETS[3]
);

const int DEVICE_ID       = 1026;
const int SENSOR_TEMP_ID  = 15;
const int SENSOR_HUM_ID   = 16;

const unsigned long PUBLISH_MS = 1000;  // publish every 1 s (DHT11 minimum ~1 s between reads)
// =======================================================

WiFiClient wifiClient;
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
      Serial.println("No IP (0.0.0.0) — check SSID/password, 2.4 GHz Wi-Fi, router DHCP");
      delay(3000);
    }
  }

  Serial.print("Board IP: ");
  Serial.println(WiFi.localIP());
}

bool testTcpToBroker() {
  Serial.print("TCP probe ");
  WiFiClient probe;
  if (probe.connect(mqttHost, MQTT_PORT)) {
    Serial.println("OK");
    probe.stop();
    return true;
  }
  Serial.println("FAIL — run Add-MosquittoFirewallRules.ps1 as Admin, or check router AP isolation");
  return false;
}

void connectMqtt() {
  mqttClient.setId("uno_r4_local");
  Serial.print("MQTT -> ");
  Serial.println(mqttHost);

  while (true) {
    if (!testTcpToBroker()) {
      delay(5000);
      continue;
    }
    if (mqttClient.connect(mqttHost, MQTT_PORT)) {
      break;
    }
    Serial.print("  MQTT retry, err=");
    Serial.println(mqttClient.connectError());
    delay(3000);
  }
  Serial.println("MQTT connected");
}

bool publishReading(const char* topic, float value) {
  char payload[48];
  snprintf(payload, sizeof(payload), "{\"value\":%.2f}", value);
  mqttClient.beginMessage(topic, false, 1);  // QoS 1 (matches backend ingest)
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
