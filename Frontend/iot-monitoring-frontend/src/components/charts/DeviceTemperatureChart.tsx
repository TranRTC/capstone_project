import React, { useEffect, useState, useCallback } from 'react';
import { Paper, Typography, Box, CircularProgress } from '@mui/material';
import { apiService } from '../../services/api';
import { signalRService } from '../../services/signalRService';
import RealTimeChart from './RealTimeChart';
import { Sensor, SensorReading } from '../../types';
import * as signalR from '@microsoft/signalr';

interface DeviceTemperatureChartProps {
  deviceId: number;
  deviceName: string;
  height?: number;
  showPaper?: boolean;
}

interface ChartDataPoint {
  timestamp: string;
  value: number;
}

const DeviceTemperatureChart: React.FC<DeviceTemperatureChartProps> = ({
  deviceId,
  deviceName,
  height = 300,
  showPaper = true,
}) => {
  const [temperatureSensor, setTemperatureSensor] = useState<Sensor | null>(null);
  const [chartData, setChartData] = useState<ChartDataPoint[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Fetch temperature sensor for this device
  const fetchTemperatureSensor = useCallback(async () => {
    try {
      const sensors = await apiService.getSensorsByDevice(deviceId);
      // Find temperature sensor (case-insensitive)
      const tempSensor = sensors.find(
        (s) => s.sensorType.toLowerCase().includes('temperature') || 
               s.sensorType.toLowerCase().includes('temp')
      );
      
      if (tempSensor) {
        setTemperatureSensor(tempSensor);
        return tempSensor;
      } else {
        setError('No temperature sensor found for this device');
        return null;
      }
    } catch (err: any) {
      console.error('Error fetching sensors:', err);
      setError('Failed to load sensors');
      return null;
    }
  }, [deviceId]);

  // Fetch recent temperature readings
  const fetchRecentReadings = useCallback(async (sensorId: number) => {
    try {
      const endDate = new Date();
      const startDate = new Date();
      startDate.setHours(startDate.getHours() - 1); // Last hour of data

      const result = await apiService.getSensorReadings({
        deviceId,
        sensorId,
        startDate: startDate.toISOString(),
        endDate: endDate.toISOString(),
        pageSize: 100, // Get up to 100 recent readings
      });

      const readings: ChartDataPoint[] = result.items
        .filter((reading) => reading.sensorId === sensorId)
        .map((reading) => ({
          timestamp: reading.timestamp,
          value: reading.value,
        }))
        .sort((a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime());

      setChartData(readings);
      setLoading(false);
    } catch (err: any) {
      console.error('Error fetching readings:', err);
      setError('Failed to load temperature readings');
      setLoading(false);
    }
  }, [deviceId]);

  // Initialize: fetch sensor and readings
  useEffect(() => {
    const initialize = async () => {
      setLoading(true);
      setError(null);
      const sensor = await fetchTemperatureSensor();
      if (sensor) {
        await fetchRecentReadings(sensor.sensorId);
      } else {
        setLoading(false);
      }
    };

    initialize();
  }, [deviceId, fetchTemperatureSensor, fetchRecentReadings]);

  // Set up SignalR for real-time updates
  useEffect(() => {
    if (!temperatureSensor) return;

    const setupSignalR = async () => {
      try {
        // Ensure SignalR is connected
        const state = signalRService.getConnectionState();
        if (state !== signalR.HubConnectionState.Connected) {
          await signalRService.start();
        }

        // Subscribe to sensor updates
        await signalRService.subscribeToSensor(temperatureSensor.sensorId);
        await signalRService.subscribeToDevice(deviceId);

        // Listen for new sensor readings
        const handleNewReading = (reading: SensorReading) => {
          // Only update if this reading is for our sensor
          if (reading.sensorId === temperatureSensor.sensorId && reading.deviceId === deviceId) {
            setChartData((prev) => {
              const newData = [
                ...prev,
                {
                  timestamp: reading.timestamp,
                  value: reading.value,
                },
              ];
              // Keep only last 50 data points for performance
              return newData.slice(-50);
            });
          }
        };

        signalRService.onSensorReading(handleNewReading);

        // Cleanup
        return () => {
          signalRService.off('SensorReadingReceived');
        };
      } catch (err) {
        console.error('Error setting up SignalR:', err);
      }
    };

    setupSignalR();
  }, [temperatureSensor, deviceId]);

  const content = (
    <>
      {showPaper && (
        <>
          <Typography variant="h6" gutterBottom>
            {deviceName} - Temperature
          </Typography>
          <Typography variant="body2" color="textSecondary" gutterBottom>
            Sensor: {temperatureSensor?.sensorName} ({temperatureSensor?.sensorType})
          </Typography>
        </>
      )}
      {loading ? (
        <Box sx={{ height, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
          <CircularProgress />
        </Box>
      ) : error ? (
        <Box sx={{ height, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
          <Typography color="error">{error}</Typography>
        </Box>
      ) : !temperatureSensor ? (
        <Box sx={{ height, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
          <Typography color="textSecondary">No temperature sensor available</Typography>
        </Box>
      ) : chartData.length === 0 ? (
        <Box sx={{ height, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
          <Typography color="textSecondary">No temperature data available yet</Typography>
        </Box>
      ) : (
        <RealTimeChart
          data={chartData}
          maxDataPoints={50}
          name="Temperature"
          color="#e74c3c"
          height={height}
          unit={temperatureSensor.unit || '°C'}
        />
      )}
    </>
  );

  if (showPaper) {
    return <Paper sx={{ p: 3 }}>{content}</Paper>;
  }

  return <Box>{content}</Box>;
};

export default DeviceTemperatureChart;

