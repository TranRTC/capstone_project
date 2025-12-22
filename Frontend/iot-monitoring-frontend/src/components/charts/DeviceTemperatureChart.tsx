import React, { useEffect, useState, useCallback } from 'react';
import { 
  Paper, 
  Typography, 
  Box, 
  CircularProgress, 
  ButtonGroup, 
  Button, 
  IconButton,
  Tooltip,
  Slider,
} from '@mui/material';
import { ZoomIn, ZoomOut, RestartAlt } from '@mui/icons-material';
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
  // Chart window configuration
  windowMode?: 'time' | 'points'; // 'time' = SCADA style (time-based), 'points' = data points based
  timeWindowMinutes?: number; // Time window in minutes (used when windowMode='time')
  maxDataPoints?: number; // Max data points (used when windowMode='points')
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
  windowMode: initialWindowMode = 'points', // Default to points mode for backward compatibility
  timeWindowMinutes: initialTimeWindowMinutes = 5, // Default 5 minutes for time mode
  maxDataPoints = 50, // Default 50 points for points mode
}) => {
  const [temperatureSensor, setTemperatureSensor] = useState<Sensor | null>(null);
  const [chartData, setChartData] = useState<ChartDataPoint[]>([]);
  const [currentValue, setCurrentValue] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  // Zoom state - allow dynamic zoom control
  const [windowMode, setWindowMode] = useState<'time' | 'points'>(initialWindowMode);
  const [timeWindowMinutes, setTimeWindowMinutes] = useState(initialTimeWindowMinutes);
  
  // Preset zoom levels (in minutes)
  const zoomPresets = [
    { label: '2s', value: 2 / 60 }, // 2 seconds
    { label: '5s', value: 5 / 60 }, // 5 seconds
    { label: '10s', value: 10 / 60 }, // 10 seconds
    { label: '30s', value: 0.5 }, // 30 seconds
    { label: '1m', value: 1 }, // 1 minute
    { label: '2m', value: 2 }, // 2 minutes
    { label: '5m', value: 5 }, // 5 minutes
    { label: '10m', value: 10 }, // 10 minutes
  ];

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
      // Set current value to the latest reading
      if (readings.length > 0) {
        setCurrentValue(readings[readings.length - 1].value);
      }
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
            setCurrentValue(reading.value);
            setChartData((prev) => {
              // Add new reading to the end (right side)
              const newData = [
                ...prev,
                {
                  timestamp: reading.timestamp,
                  value: reading.value,
                },
              ];
              
              // Remove duplicates based on timestamp
              const uniqueData = Array.from(
                new Map(newData.map(item => [item.timestamp, item])).values()
              );
              
              // Sort by timestamp (oldest first = left, newest last = right)
              const sortedData = uniqueData.sort(
                (a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()
              );
              
              // Apply window based on mode
              if (windowMode === 'time') {
                // Time-based window (SCADA style): filter by time, no point limit
                const now = Date.now();
                const windowMs = timeWindowMinutes * 60 * 1000;
                return sortedData.filter(
                  (point) => (now - new Date(point.timestamp).getTime()) <= windowMs
                );
              } else {
                // Points-based window: limit by number of points
                if (sortedData.length > maxDataPoints) {
                  // Remove the first (oldest/leftmost) point, keep the last maxDataPoints
                  return sortedData.slice(-maxDataPoints);
                }
                // If not full yet, return all data (will fill from left to right)
                return sortedData;
              }
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
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
            <Typography variant="h6" gutterBottom>
              {deviceName} - Temperature
            </Typography>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <Typography variant="caption" color="textSecondary" sx={{ mr: 1 }}>
                {windowMode === 'time' 
                  ? timeWindowMinutes < 1 
                    ? `Window: Last ${(timeWindowMinutes * 60).toFixed(0)}s` 
                    : `Window: Last ${timeWindowMinutes.toFixed(1)}m`
                  : `Window: Last ${maxDataPoints} points`}
              </Typography>
            </Box>
          </Box>
          <Typography variant="body2" color="textSecondary" gutterBottom>
            Sensor: {temperatureSensor?.sensorName} ({temperatureSensor?.sensorType})
          </Typography>
        </>
      )}
      
      {/* Zoom Controls - Only show in time mode */}
      {!loading && !error && temperatureSensor && windowMode === 'time' && (
        <Box sx={{ mb: 2, p: 1, bgcolor: 'background.default', borderRadius: 1 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
            <Typography variant="caption" sx={{ minWidth: '60px' }}>
              Zoom:
            </Typography>
            <ButtonGroup size="small" variant="outlined">
              {zoomPresets.map((preset) => (
                <Tooltip key={preset.label} title={`${preset.label} window`}>
                  <Button
                    onClick={() => setTimeWindowMinutes(preset.value)}
                    variant={Math.abs(timeWindowMinutes - preset.value) < 0.001 ? 'contained' : 'outlined'}
                    sx={{ minWidth: '40px', fontSize: '0.7rem' }}
                  >
                    {preset.label}
                  </Button>
                </Tooltip>
              ))}
            </ButtonGroup>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, ml: 'auto', minWidth: '200px' }}>
              <ZoomOut fontSize="small" />
              <Slider
                value={timeWindowMinutes}
                onChange={(_, value) => setTimeWindowMinutes(value as number)}
                min={2 / 60} // 2 seconds minimum
                max={30} // 30 minutes maximum
                step={1 / 60} // 1 second steps
                valueLabelDisplay="auto"
                valueLabelFormat={(value) => 
                  value < 1 ? `${(value * 60).toFixed(0)}s` : `${value.toFixed(1)}m`
                }
                sx={{ width: '150px' }}
              />
              <ZoomIn fontSize="small" />
            </Box>
            <Tooltip title="Reset to default (5 minutes)">
              <IconButton 
                size="small" 
                onClick={() => setTimeWindowMinutes(initialTimeWindowMinutes)}
              >
                <RestartAlt fontSize="small" />
              </IconButton>
            </Tooltip>
          </Box>
        </Box>
      )}
      {/* Current Value Display */}
      {!loading && !error && temperatureSensor && currentValue !== null && (
        <Box
          sx={{
            mb: 2,
            p: 2,
            bgcolor: 'primary.light',
            borderRadius: 2,
            textAlign: 'center',
          }}
        >
          <Typography variant="body2" color="text.secondary" gutterBottom>
            Current Temperature
          </Typography>
          <Typography
            variant="h3"
            component="div"
            sx={{
              fontWeight: 'bold',
              color: 'primary.main',
            }}
          >
            {currentValue.toFixed(2)} {temperatureSensor.unit || '°C'}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            Last updated: {chartData.length > 0 
              ? new Date(chartData[chartData.length - 1].timestamp).toLocaleTimeString()
              : 'N/A'}
          </Typography>
        </Box>
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
          maxDataPoints={maxDataPoints}
          timeWindowMinutes={windowMode === 'time' ? timeWindowMinutes : undefined}
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

