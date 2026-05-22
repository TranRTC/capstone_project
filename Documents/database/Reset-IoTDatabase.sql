/*
  Reset IoTMonitoringDB — delete all device/sensor/reading data and restart IDs at 1

  WHEN TO USE:
  - Local SQL Server / LocalDB: database name IoTMonitoringDB (see appsettings.json)
  - Run separately on CLOUD Azure SQL if you want the same clean slate there

  BEFORE RUNNING:
  1. Stop the backend API (dotnet run) so nothing is writing to the DB.
  2. Close SSMS queries that might lock tables.
  3. Back up first if you might need old data.

  AFTER RUNNING:
  1. Start the API again — it will re-apply migrations and seed admin if Users is empty.
  2. Log in: admin / Admin@123 (from appsettings AdminSeed).
  3. Create Device #1, then sensors #1, #2, … in the UI.
  4. Update Arduino sketches: DEVICE_ID=1, SENSOR_TEMP_ID=1, etc.
*/

USE [IoTMonitoringDB];
GO

SET NOCOUNT ON;

PRINT 'Deleting rows (child tables first)...';

DELETE FROM [Alerts];
DELETE FROM [AlertRules];
DELETE FROM [SensorReadings];
DELETE FROM [DeviceCommands];
DELETE FROM [Actuators];
DELETE FROM [Sensors];
DELETE FROM [DeviceConfigurations];
DELETE FROM [DeviceStatusHistories];
DELETE FROM [OperationalMetrics];
DELETE FROM [Devices];

-- Remove users so the API seeds a fresh admin on next startup (optional: comment out to keep logins)
DELETE FROM [Users];

PRINT 'Resetting IDENTITY seeds (next insert will be 1)...';

DBCC CHECKIDENT ('[Alerts]', RESEED, 0);
DBCC CHECKIDENT ('[AlertRules]', RESEED, 0);
DBCC CHECKIDENT ('[SensorReadings]', RESEED, 0);
DBCC CHECKIDENT ('[DeviceCommands]', RESEED, 0);
DBCC CHECKIDENT ('[Actuators]', RESEED, 0);
DBCC CHECKIDENT ('[Sensors]', RESEED, 0);
DBCC CHECKIDENT ('[DeviceConfigurations]', RESEED, 0);
DBCC CHECKIDENT ('[DeviceStatusHistories]', RESEED, 0);
DBCC CHECKIDENT ('[OperationalMetrics]', RESEED, 0);
DBCC CHECKIDENT ('[Devices]', RESEED, 0);
DBCC CHECKIDENT ('[Users]', RESEED, 0);

PRINT 'Done. Row counts:';

SELECT 'Devices' AS [Table], COUNT(*) AS [Rows] FROM [Devices]
UNION ALL SELECT 'Sensors', COUNT(*) FROM [Sensors]
UNION ALL SELECT 'SensorReadings', COUNT(*) FROM [SensorReadings]
UNION ALL SELECT 'Actuators', COUNT(*) FROM [Actuators]
UNION ALL SELECT 'Users', COUNT(*) FROM [Users];

GO
