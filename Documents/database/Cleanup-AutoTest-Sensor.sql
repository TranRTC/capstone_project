/*
  One-off cleanup when DELETE sensor fails for "API Test Device" / auto-test data.
  Replace @SensorId with the sensor you cannot delete (from the UI).

  Safer after API fix: restart API and delete from the UI instead.
*/

DECLARE @SensorId INT = 1;  -- <-- change this

USE [IoTMonitoringDB];

DECLARE @RuleIds TABLE (AlertRuleId INT);
INSERT INTO @RuleIds SELECT AlertRuleId FROM AlertRules WHERE SensorId = @SensorId;

DELETE FROM Alerts WHERE AlertRuleId IN (SELECT AlertRuleId FROM @RuleIds);
DELETE FROM AlertRules WHERE SensorId = @SensorId;
DELETE FROM SensorReadings WHERE SensorId = @SensorId;
UPDATE Actuators SET FeedbackSensorId = NULL WHERE FeedbackSensorId = @SensorId;
DELETE FROM Sensors WHERE SensorId = @SensorId;

SELECT 'Remaining rows for sensor' AS Info, @SensorId AS SensorId;
SELECT COUNT(*) AS Readings FROM SensorReadings WHERE SensorId = @SensorId;
SELECT COUNT(*) AS Rules FROM AlertRules WHERE SensorId = @SensorId;
