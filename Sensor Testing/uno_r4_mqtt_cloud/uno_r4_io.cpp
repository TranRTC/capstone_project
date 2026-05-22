#include "uno_r4_io.h"
#include <math.h>
#include <string.h>

static int pwmFromPercent(float percent) {
  if (percent < 0.0f) percent = 0.0f;
  if (percent > 100.0f) percent = 100.0f;
  return (int)lroundf((percent / 100.0f) * 255.0f);
}

static float mapAnalogCommand(float value, float minV, float maxV) {
  if (maxV <= minV) return value;
  if (value < minV) value = minV;
  if (value > maxV) value = maxV;
  return ((value - minV) / (maxV - minV)) * 100.0f;
}

static long extractJsonLong(const char* s, const char* key) {
  char pattern[24];
  snprintf(pattern, sizeof(pattern), "\"%s\":", key);
  const char* p = strstr(s, pattern);
  if (!p) return -1;
  p += strlen(pattern);
  while (*p == ' ') p++;
  return strtol(p, nullptr, 10);
}

static bool extractJsonBool(const char* s, const char* key) {
  char pattern[24];
  snprintf(pattern, sizeof(pattern), "\"%s\":", key);
  const char* p = strstr(s, pattern);
  if (!p) return false;
  p += strlen(pattern);
  while (*p == ' ') p++;
  return strncmp(p, "true", 4) == 0;
}

static int extractChannelPin(const char* s) {
  const char* p = strstr(s, "\"channel\":");
  if (!p) return -1;
  p += 10;
  while (*p == ' ' || *p == '\"') p++;
  return (int)strtol(p, nullptr, 10);
}

static float extractPayloadValue(const char* s) {
  const char* p = strstr(s, "\"payload\":");
  if (!p) return NAN;
  p = strstr(p, "\"value\":");
  if (!p) return NAN;
  p += 8;
  while (*p == ' ') p++;
  return strtof(p, nullptr);
}

static bool extractPayloadOn(const char* s) {
  const char* p = strstr(s, "\"payload\":");
  if (!p) return false;
  return extractJsonBool(p, "on");
}

static bool isSetPower(const char* s) {
  const char* p = strstr(s, "\"commandType\":");
  if (!p) return false;
  return strstr(p, "SetPower") != nullptr;
}

static bool isSetValue(const char* s) {
  const char* p = strstr(s, "\"commandType\":");
  if (!p) return false;
  return strstr(p, "SetValue") != nullptr;
}

static void applyDigitalOutput(int pin, bool on) {
  digitalWrite(pin, on ? HIGH : LOW);
}

static void applyAnalogOutput(int pin, float percent) {
  analogWrite(pin, pwmFromPercent(percent));
}

static bool pinMatchesChannel(int pin, int channel) {
  return channel >= 0 && pin == channel;
}

void unoR4IoBegin() {
  pinMode(IO_PIN_DI1, INPUT_PULLUP);
  pinMode(IO_PIN_DI2, INPUT_PULLUP);
  pinMode(IO_PIN_DO1, OUTPUT);
  pinMode(IO_PIN_DO2, OUTPUT);
  pinMode(IO_PIN_AO1, OUTPUT);
  digitalWrite(IO_PIN_DO1, LOW);
  digitalWrite(IO_PIN_DO2, LOW);
  analogWrite(IO_PIN_AO1, 0);
}

void unoR4IoBuildSensorTopics(const UnoR4IoConfig& cfg, char* topicDi1, char* topicDi2, size_t len) {
  snprintf(topicDi1, len, "devices/%d/sensors/%d/readings", cfg.deviceId, cfg.sensorDi1Id);
  snprintf(topicDi2, len, "devices/%d/sensors/%d/readings", cfg.deviceId, cfg.sensorDi2Id);
}

void unoR4IoBuildCommandTopic(int deviceId, char* buf, size_t len) {
  snprintf(buf, len, "devices/%d/commands", deviceId);
}

void unoR4IoBuildAckTopic(int deviceId, char* buf, size_t len) {
  snprintf(buf, len, "devices/%d/commands/ack", deviceId);
}

void unoR4IoPublishDigitalInputs(
  const char* topicDi1,
  const char* topicDi2,
  bool (*publishReading)(const char* topic, float value)) {
  float di1 = digitalRead(IO_PIN_DI1) == HIGH ? 1.0f : 0.0f;
  float di2 = digitalRead(IO_PIN_DI2) == HIGH ? 1.0f : 0.0f;
  publishReading(topicDi1, di1);
  publishReading(topicDi2, di2);
}

bool unoR4IoHandleCommandJson(
  const char* json,
  size_t len,
  const UnoR4IoConfig& cfg,
  void (*publishAck)(const char* ackJson)) {
  if (!json || len == 0) return false;

  long commandId = extractJsonLong(json, "commandId");
  if (commandId < 0) return false;

  int channel = extractChannelPin(json);
  char ack[128];

  if (isSetPower(json)) {
    bool on = extractPayloadOn(json);
    bool applied = false;

    if (pinMatchesChannel(IO_PIN_DO1, channel)) {
      applyDigitalOutput(IO_PIN_DO1, on);
      applied = true;
    } else if (pinMatchesChannel(IO_PIN_DO2, channel)) {
      applyDigitalOutput(IO_PIN_DO2, on);
      applied = true;
    }

    if (applied) {
      snprintf(ack, sizeof(ack), "{\"commandId\":%ld,\"status\":\"Acked\"}", commandId);
      publishAck(ack);
      return true;
    }

    snprintf(ack, sizeof(ack),
             "{\"commandId\":%ld,\"status\":\"Failed\",\"errorMessage\":\"Unknown DO channel\"}",
             commandId);
    publishAck(ack);
    return true;
  }

  if (isSetValue(json)) {
    float value = extractPayloadValue(json);
    if (isnan(value)) {
      snprintf(ack, sizeof(ack),
               "{\"commandId\":%ld,\"status\":\"Failed\",\"errorMessage\":\"Missing value\"}",
               commandId);
      publishAck(ack);
      return true;
    }

    float percent = mapAnalogCommand(value, cfg.analogOutMin, cfg.analogOutMax);
    bool applied = false;

    if (pinMatchesChannel(IO_PIN_AO1, channel)) {
      applyAnalogOutput(IO_PIN_AO1, percent);
      applied = true;
    }

    if (applied) {
      snprintf(ack, sizeof(ack), "{\"commandId\":%ld,\"status\":\"Acked\"}", commandId);
      publishAck(ack);
      return true;
    }

    snprintf(ack, sizeof(ack),
             "{\"commandId\":%ld,\"status\":\"Failed\",\"errorMessage\":\"Unknown AO channel\"}",
             commandId);
    publishAck(ack);
    return true;
  }

  return false;
}
