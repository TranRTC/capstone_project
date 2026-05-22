/*
  FORCE reset — use if the normal script ran but IDs/data look unchanged.
  Run in the SAME server shown by Verify-Which-Database.ps1 (usually (localdb)\mssqllocaldb).

  Stop dotnet run before executing.
*/

USE [master];
GO

IF DB_ID(N'IoTMonitoringDB') IS NULL
BEGIN
    RAISERROR('Database IoTMonitoringDB does not exist on this server. Create it with: dotnet ef database update', 16, 1);
    RETURN;
END
GO

ALTER DATABASE [IoTMonitoringDB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
GO

USE [IoTMonitoringDB];
GO

-- Delete all application data
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
DELETE FROM [Users];

-- Reseed identities (next INSERT gets 1)
DBCC CHECKIDENT ('[Devices]', RESEED, 0);
DBCC CHECKIDENT ('[Sensors]', RESEED, 0);
DBCC CHECKIDENT ('[Actuators]', RESEED, 0);
DBCC CHECKIDENT ('[SensorReadings]', RESEED, 0);
DBCC CHECKIDENT ('[DeviceCommands]', RESEED, 0);
DBCC CHECKIDENT ('[AlertRules]', RESEED, 0);
DBCC CHECKIDENT ('[Alerts]', RESEED, 0);
DBCC CHECKIDENT ('[DeviceConfigurations]', RESEED, 0);
DBCC CHECKIDENT ('[DeviceStatusHistories]', RESEED, 0);
DBCC CHECKIDENT ('[OperationalMetrics]', RESEED, 0);
DBCC CHECKIDENT ('[Users]', RESEED, 0);

ALTER DATABASE [IoTMonitoringDB] SET MULTI_USER;
GO

USE [IoTMonitoringDB];
GO

SELECT @@SERVERNAME AS ServerName, DB_NAME() AS DatabaseName;
SELECT DeviceId, DeviceName FROM Devices;
SELECT SensorId, DeviceId, SensorName FROM Sensors;
SELECT
  IDENT_CURRENT('Devices') AS CurrentIdentityDevices,
  IDENT_CURRENT('Sensors') AS CurrentIdentitySensors;
GO
