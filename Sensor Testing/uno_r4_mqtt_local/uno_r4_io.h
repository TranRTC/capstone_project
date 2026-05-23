#pragma once

#include <Arduino.h>

// Pin map (DHT11 uses pin 2 — do not use for DO/AO)
#define IO_PIN_DI1 4
#define IO_PIN_DO1 7
#define IO_PIN_DO2 5   // digital output (SetPower channel = pin number)
#define IO_PIN_AO1 3   // PWM capable

struct UnoR4IoConfig {
  int deviceId;
  int sensorDi1Id;
  float analogOutMin;
  float analogOutMax;
};

void unoR4IoBegin();
void unoR4IoBuildSensorTopicDi1(const UnoR4IoConfig& cfg, char* topicDi1, size_t len);
void unoR4IoBuildCommandTopic(int deviceId, char* buf, size_t len);
void unoR4IoBuildAckTopic(int deviceId, char* buf, size_t len);

void unoR4IoPublishDigitalInput(
  const char* topicDi1,
  bool (*publishReading)(const char* topic, float value));

// Returns true if a command was handled
bool unoR4IoHandleCommandJson(
  const char* json,
  size_t len,
  const UnoR4IoConfig& cfg,
  void (*publishAck)(const char* ackJson));
