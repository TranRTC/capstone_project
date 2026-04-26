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
  Alert,
} from '@mui/material';
import { alpha, useTheme } from '@mui/material/styles';
import { ZoomIn, ZoomOut, RestartAlt } from '@mui/icons-material';
import { apiService } from '../../services/api';
import { signalRService } from '../../services/signalRService';
import RealTimeChart from './RealTimeChart';
import { Sensor, SensorReading } from '../../types';
import * as signalR from '@microsoft/signalr';

interface DeviceTemperatureChartProps {
  deviceId: number;
  deviceName: string;
  /** When set, chart this sensor only. Otherwise picks first type matching temperature/temp. */
  sensorId?: number;
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
  sensorId,
  height = 300,
  showPaper = true,
  windowMode: initialWindowMode = 'points', // Default to points mode for backward compatibility
  timeWindowMinutes: initialTimeWindowMinutes = 5, // Default 5 minutes for time mode
  maxDataPoints = 50, // Default 50 points for points mode
}) => {
  const theme = useTheme();
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

  const fetchTemperatureSensor = useCallback(async () => {
    try {
      const sensors = await apiService.getSensorsByDevice(deviceId);

      const chartSensor =
        sensorId != null
          ? sensors.find((s) => s.sensorId === sensorId)
          : sensors.find(
              (s) =>
                s.sensorType.toLowerCase().includes('temperature') ||
                s.sensorType.toLowerCase().includes('temp')
            );

      if (chartSensor) {
        setTemperatureSensor(chartSensor);
        return chartSensor;
      }

      setError(
        sensorId != null
          ? 'Selected sensor is not available for this device'
          : 'No temperature sensor found for this device'
      );
      return null;
    } catch (err: any) {
      console.error('Error fetching sensors:', err);
      setError('Failed to load sensors');
      return null;
    }
  }, [deviceId, sensorId]);

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
      setError('Failed to load sensor readings');
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
  }, [deviceId, sensorId, fetchTemperatureSensor, fetchRecentReadings]);

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
              {deviceName} — {temperatureSensor?.sensorName ?? 'Sensor'}
            </Typography>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <Typography variant="caption" color="text.secondary" sx={{ mr: 1 }}>
                {windowMode === 'time' 
                  ? timeWindowMinutes < 1 
                    ? `Window: Last ${(timeWindowMinutes * 60).toFixed(0)}s` 
                    : `Window: Last ${timeWindowMinutes.toFixed(1)}m`
                  : `Window: Last ${maxDataPoints} points`}
              </Typography>
            </Box>
          </Box>
          <Typography variant="body2" color="text.secondary" gutterBottom>
            {temperatureSensor?.sensorType}
          </Typography>
        </>
      )}
      
      {/* Zoom Controls - Only show in time mode */}
      {!loading && !error && temperatureSensor && windowMode === 'time' && (
        <Box
          sx={{
            mb: 2,
            p: 1.5,
            border: 1,
            borderColor: 'divider',
            borderRadius: 2,
            bgcolor: 'background.default',
          }}
        >
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
            <Typography variant="caption" color="text.secondary" sx={{ minWidth: 48, fontWeight: 600 }}>
              Window
            </Typography>
            <ButtonGroup size="small" variant="outlined" sx={{ flexWrap: 'wrap' }}>
              {zoomPresets.map((preset) => (
                <Tooltip key={preset.label} title={`Show last ${preset.label}`}>
                  <Button
                    onClick={() => setTimeWindowMinutes(preset.value)}
                    variant={Math.abs(timeWindowMinutes - preset.value) < 0.001 ? 'contained' : 'outlined'}
                    sx={{
                      minWidth: 36,
                      px: 0.75,
                      fontSize: '0.7rem',
                      fontWeight: 600,
                      borderColor: 'divider',
                    }}
                  >
                    {preset.label}
                  </Button>
                </Tooltip>
              ))}
            </ButtonGroup>
            <Box
              sx={{
                display: 'flex',
                alignItems: 'center',
                gap: 1,
                ml: { xs: 0, sm: 'auto' },
                minWidth: { xs: '100%', sm: 200 },
                flex: { xs: '1 1 100%', sm: '0 1 auto' },
              }}
            >
              <ZoomOut sx={{ fontSize: 18, color: 'text.secondary' }} />
              <Slider
                value={timeWindowMinutes}
                onChange={(_, value) => setTimeWindowMinutes(value as number)}
                min={2 / 60}
                max={30}
                step={1 / 60}
                valueLabelDisplay="auto"
                size="small"
                valueLabelFormat={(value) =>
                  value < 1 ? `${(value * 60).toFixed(0)}s` : `${value.toFixed(1)}m`
                }
                sx={{ flex: 1, maxWidth: 220, color: theme.palette.primary.main }}
              />
              <ZoomIn sx={{ fontSize: 18, color: 'text.secondary' }} />
            </Box>
            <Tooltip title="Reset window">
              <IconButton size="small" onClick={() => setTimeWindowMinutes(initialTimeWindowMinutes)} sx={{ ml: 'auto' }}>
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
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            gap: 2,
            flexWrap: 'wrap',
            py: 1.75,
            px: 2,
            borderRadius: 2,
            border: 1,
            borderColor: 'divider',
            bgcolor: alpha(theme.palette.primary.main, 0.04),
            borderLeft: 4,
            borderLeftColor: 'primary.main',
          }}
        >
          <Box>
            <Typography variant="overline" color="text.secondary" sx={{ letterSpacing: 0.5, lineHeight: 1.6 }}>
              Latest reading
            </Typography>
            <Box sx={{ display: 'flex', alignItems: 'baseline', gap: 0.75, mt: 0.25 }}>
              <Typography variant="h4" component="span" sx={{ fontWeight: 700, letterSpacing: '-0.02em' }}>
                {currentValue.toFixed(2)}
              </Typography>
              <Typography variant="body1" color="text.secondary" component="span">
                {temperatureSensor.unit || '°C'}
              </Typography>
            </Box>
          </Box>
          <Typography variant="caption" color="text.secondary" sx={{ textAlign: { xs: 'left', sm: 'right' } }}>
            {chartData.length > 0
              ? new Date(chartData[chartData.length - 1].timestamp).toLocaleString()
              : '—'}
          </Typography>
        </Box>
      )}
      {loading ? (
        <Box
          sx={{
            height,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            border: 1,
            borderColor: 'divider',
            borderRadius: 2,
            bgcolor: 'background.default',
          }}
        >
          <CircularProgress size={36} thickness={4} />
        </Box>
      ) : error ? (
        <Box sx={{ minHeight: height * 0.35, display: 'flex', alignItems: 'center' }}>
          <Alert severity="error" sx={{ width: '100%' }}>
            {error}
          </Alert>
        </Box>
      ) : !temperatureSensor ? (
        <Box
          sx={{
            height,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            border: 1,
            borderColor: 'divider',
            borderRadius: 2,
            bgcolor: 'background.default',
          }}
        >
          <Typography color="text.secondary" variant="body2">
            No chart sensor available
          </Typography>
        </Box>
      ) : chartData.length === 0 ? (
        <Box
          sx={{
            height,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            border: 1,
            borderColor: 'divider',
            borderRadius: 2,
            bgcolor: 'background.default',
          }}
        >
          <Typography color="text.secondary" variant="body2">
            No readings for this sensor yet
          </Typography>
        </Box>
      ) : (
        <Box
          sx={{
            p: { xs: 1, sm: 2 },
            border: 1,
            borderColor: 'divider',
            borderRadius: 2,
            bgcolor: 'background.paper',
          }}
        >
          <RealTimeChart
            data={chartData}
            maxDataPoints={maxDataPoints}
            timeWindowMinutes={windowMode === 'time' ? timeWindowMinutes : undefined}
            name={temperatureSensor?.sensorName ?? 'Sensor'}
            height={height}
            unit={temperatureSensor.unit || '°C'}
          />
        </Box>
      )}
    </>
  );

  if (showPaper) {
    return <Paper sx={{ p: 3 }}>{content}</Paper>;
  }

  return <Box>{content}</Box>;
};

export default DeviceTemperatureChart;

