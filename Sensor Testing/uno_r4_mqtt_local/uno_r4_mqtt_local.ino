/*
 * Arduino Uno R4 WiFi — LOCAL Mosquitto
 *
 * Sensors (publish):
 *   DHT11 pin 2 — temp / humidity
 *   Digital input pin 4 — 0/1 (INPUT_PULLUP)
 *
 * Actuators (subscribe devices/{id}/commands):
 *   Digital outputs pin 7, 5 — SetPower, channel = pin number
 *   Analog output pin 3 (PWM) — SetValue, channel = 3
 *
 * Create matching sensors/actuators in the dashboard (see README).
 */

#include <WiFiS3.h>
#include <ArduinoMqttClient.h>
#include <DHT.h>
#include "uno_r4_io.h"

#define DHT_PIN 2
DHT dht(DHT_PIN, DHT11);

// ===================== EDIT CONFIG =====================
const char* WIFI_SSID     = "trandiep";
const char* WIFI_PASSWORD = "bingchilling@3614";

const int MQTT_HOST_OCTETS[] = { 192, 168, 0, 112 };
const int   MQTT_PORT        = 1883;

IPAddress mqttHost(
  MQTT_HOST_OCTETS[0],
  MQTT_HOST_OCTETS[1],
  MQTT_HOST_OCTETS[2],
  MQTT_HOST_OCTETS[3]
);

// Match IDs after a fresh DB reset (device 1, sensors 1–3) — see Documents/database/README-Reset-Database.md
const int DEVICE_ID       = 1;
const int SENSOR_TEMP_ID  = 1;
const int SENSOR_HUM_ID   = 2;
const int SENSOR_DI1_ID   = 3;

const unsigned long PUBLISH_MS = 1000;
// =======================================================

WiFiClient wifiClient;
MqttClient mqttClient(wifiClient);

UnoR4IoConfig ioConfig = {
  DEVICE_ID,
  SENSOR_DI1_ID,
  0.0f,
  100.0f
};

char topicTemp[64];
char topicHum[64];
char topicDi1[64];
char topicCommands[48];
char topicAck[48];

unsigned long lastPublish = 0;
bool commandTopicReady = false;

const unsigned long DHCP_TIMEOUT_MS = 20000;

void onMqttMessage(int messageSize);

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
  unoR4IoBuildSensorTopicDi1(ioConfig, topicDi1, sizeof(topicDi1));
  unoR4IoBuildCommandTopic(DEVICE_ID, topicCommands, sizeof(topicCommands));
  unoR4IoBuildAckTopic(DEVICE_ID, topicAck, sizeof(topicAck));
}

void connectWiFi() {
  Serial.print("WiFi");
  if (WiFi.status() == WL_NO_MODULE) {
    Serial.println(" - no WiFi module!");
    for (;;) delay(1000);
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
      Serial.println("No IP — check Wi-Fi");
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
  Serial.println("FAIL");
  return false;
}

void subscribeCommands() {
  mqttClient.onMessage(onMqttMessage);
  Serial.print("Subscribe ");
  Serial.println(topicCommands);
  mqttClient.subscribe(topicCommands, 1);
  commandTopicReady = true;
}

void connectMqtt() {
  mqttClient.setId("uno_r4_local_io");
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
  subscribeCommands();
}

bool publishReading(const char* topic, float value) {
  char payload[48];
  snprintf(payload, sizeof(payload), "{\"value\":%.2f}", value);
  mqttClient.beginMessage(topic, false, 1);
  mqttClient.print(payload);
  bool ok = mqttClient.endMessage() != 0;
  mqttClient.poll();
  return ok;
}

void publishAckJson(const char* ackJson) {
  Serial.print("ACK ");
  Serial.println(ackJson);
  mqttClient.beginMessage(topicAck, false, 1);
  mqttClient.print(ackJson);
  mqttClient.endMessage();
  mqttClient.poll();
}

void onMqttMessage(int messageSize) {
  char buf[512];
  int i = 0;
  while (mqttClient.available() && i < (int)sizeof(buf) - 1) {
    buf[i++] = (char)mqttClient.read();
  }
  buf[i] = '\0';

  Serial.print("CMD ");
  Serial.println(buf);
  unoR4IoHandleCommandJson(buf, i, ioConfig, publishAckJson);
}

void setup() {
  Serial.begin(9600);
  delay(2000);
  dht.begin();
  unoR4IoBegin();
  buildTopics();
  connectWiFi();
  connectMqtt();
}

void loop() {
  if (!wifiReady()) connectWiFi();
  if (wifiReady() && !mqttClient.connected()) {
    commandTopicReady = false;
    connectMqtt();
  }
  mqttClient.poll();

  if (!wifiReady() || !mqttClient.connected()) return;
  if (!commandTopicReady) subscribeCommands();

  if (millis() - lastPublish < PUBLISH_MS) return;
  lastPublish = millis();

  float tempC = dht.readTemperature();
  float hum = dht.readHumidity();
  if (!isnan(tempC)) {
    Serial.print("Temp ");
    Serial.println(publishReading(topicTemp, tempC) ? "OK" : "FAIL");
  }
  if (!isnan(hum)) {
    Serial.print("Hum ");
    Serial.println(publishReading(topicHum, hum) ? "OK" : "FAIL");
  }

  Serial.print("DI ");
  unoR4IoPublishDigitalInput(topicDi1, publishReading);
}
