/*
 * Arduino Uno R4 WiFi — CLOUD HiveMQ (TLS :8883)
 *
 * Same I/O as local sketch — see uno_r4_io.h pin map.
 * Edit DEVICE_ID / sensor IDs to match your cloud dashboard.
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

const char* MQTT_BROKER   = "d3221e515d824a45849fcffe802de489.s1.eu.hivemq.cloud";
const int   MQTT_PORT     = 8883;
const char* MQTT_USER     = "iotuser";
const char* MQTT_PASS     = "IoTCapstone2026!";

// Match IDs in cloud DB (same order as local after reset: device 1, sensors 1–4)
const int DEVICE_ID       = 1;
const int SENSOR_TEMP_ID  = 1;
const int SENSOR_HUM_ID   = 2;
const int SENSOR_DI1_ID   = 3;
const int SENSOR_DI2_ID   = 4;

const unsigned long PUBLISH_MS = 1000;
// =======================================================

WiFiSSLClient wifiClient;
MqttClient mqttClient(wifiClient);

UnoR4IoConfig ioConfig = {
  DEVICE_ID,
  SENSOR_DI1_ID,
  SENSOR_DI2_ID,
  0.0f,
  100.0f
};

char topicTemp[64];
char topicHum[64];
char topicDi1[64];
char topicDi2[64];
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
  unoR4IoBuildSensorTopics(ioConfig, topicDi1, topicDi2, sizeof(topicDi1));
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

bool testTlsToBroker() {
  Serial.print("TLS probe ");
  WiFiSSLClient probe;
  if (probe.connect(MQTT_BROKER, MQTT_PORT)) {
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
  mqttClient.setId("uno_r4_cloud_io");
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
  unoR4IoPublishDigitalInputs(topicDi1, topicDi2, publishReading);
}
