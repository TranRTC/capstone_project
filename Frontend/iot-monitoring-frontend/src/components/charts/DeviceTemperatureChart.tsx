import React, { useEffect, useState, useCallback, useMemo, useRef } from 'react';
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
  TextField,
  MenuItem,
} from '@mui/material';
import { alpha, useTheme } from '@mui/material/styles';
import { ZoomIn, ZoomOut, RestartAlt } from '@mui/icons-material';
import { apiService } from '../../services/api';
import { signalRService } from '../../services/signalRService';
import RealTimeChart from './RealTimeChart';
import SensorAnalogGauge from './SensorAnalogGauge';
import DiscreteSensorIndicator from './DiscreteSensorIndicator';
import {
  ChartDataPoint,
  trimChartPoints,
  formatRollingWindowLabel,
  formatHistoryRangeLabel,
  liveFetchPageSize,
  historyPageSize,
  HISTORY_PAGE_SIZE_CAP,
  computeLineChartYDomain,
  isLiveScrollActive,
  parseApiTimestampMs,
} from './chartTrim';
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
  /** Omit duplicate sensor title on discrete indicator when parent already titles the view (e.g. dialog). */
  hideDiscreteSensorHeading?: boolean;
}

type TrendRange = 'live' | '10m' | '1h' | '24h' | 'custom';

const DeviceTemperatureChart: React.FC<DeviceTemperatureChartProps> = ({
  deviceId,
  deviceName,
  sensorId,
  height = 300,
  showPaper = true,
  windowMode: initialWindowMode = 'points', // Default to points mode for backward compatibility
  timeWindowMinutes: initialTimeWindowMinutes = 5, // Default 5 minutes for time mode
  maxDataPoints = 50, // Default 50 points for points mode
  hideDiscreteSensorHeading = false,
}) => {
  const theme = useTheme();
  const [temperatureSensor, setTemperatureSensor] = useState<Sensor | null>(null);
  const [chartData, setChartData] = useState<ChartDataPoint[]>([]);
  const [currentValue, setCurrentValue] = useState<number | null>(null);
  const [initialLoading, setInitialLoading] = useState(true);
  const [historyFetching, setHistoryFetching] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [trendRange, setTrendRange] = useState<TrendRange>('live');
  const trendRangeRef = useRef<TrendRange>('live');
  const sensorReadingsReadyRef = useRef(false);
  const [centerTimeInput, setCenterTimeInput] = useState('');
  const [aroundMinutes, setAroundMinutes] = useState(5);
  const [customWindow, setCustomWindow] = useState<{ startDate: string; endDate: string } | null>(null);
  /** Fixed [startMs, endMs] for history presets — set at fetch/click so axis matches API window. */
  const [historyWindowMs, setHistoryWindowMs] = useState<[number, number] | null>(null);
  
  // Zoom state - allow dynamic zoom control
  const [windowMode] = useState<'time' | 'points'>(initialWindowMode);
  const [timeWindowMinutes, setTimeWindowMinutes] = useState(initialTimeWindowMinutes);
  const [liveNow, setLiveNow] = useState(() => Date.now());

  trendRangeRef.current = trendRange;

  const trimOptions = useMemo(
    () => ({
      windowMode,
      timeWindowMinutes,
      maxDataPoints,
    }),
    [windowMode, timeWindowMinutes, maxDataPoints]
  );

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

  // Fetch readings for live or history ranges
  const fetchRecentReadings = useCallback(async (
    targetSensorId: number,
    range: TrendRange,
    windowMins: number,
    opts?: {
      customWindow?: { startDate: string; endDate: string } | null;
      aroundMins?: number;
      isInitial?: boolean;
    }
  ) => {
    const isInitial = opts?.isInitial ?? false;
    if (isInitial) {
      setInitialLoading(true);
    } else {
      setHistoryFetching(true);
    }

    try {
      const endDate = new Date();
      const startDate = new Date();
      let pageSize = 100;
      const effectiveCustomWindow = opts?.customWindow ?? null;
      const effectiveAroundMins = opts?.aroundMins ?? aroundMinutes;

      if (range === 'live' && windowMode === 'time') {
        const windowMs = windowMins * 60 * 1000 * 1.1;
        startDate.setTime(endDate.getTime() - windowMs);
        pageSize = liveFetchPageSize(windowMins);
      } else if (range === '10m') {
        startDate.setMinutes(startDate.getMinutes() - 10);
        pageSize = historyPageSize('10m');
      } else if (range === '1h') {
        startDate.setHours(startDate.getHours() - 1);
        pageSize = historyPageSize('1h');
      } else if (range === '24h') {
        startDate.setHours(startDate.getHours() - 24);
        pageSize = historyPageSize('24h');
      } else if (range === 'custom' && effectiveCustomWindow != null) {
        pageSize = historyPageSize('custom', effectiveAroundMins);
      } else if (range === 'live') {
        startDate.setMinutes(startDate.getMinutes() - 10);
      }

      const resolvedStartDate =
        range === 'custom' && effectiveCustomWindow != null
          ? effectiveCustomWindow.startDate
          : startDate.toISOString();
      const resolvedEndDate =
        range === 'custom' && effectiveCustomWindow != null
          ? effectiveCustomWindow.endDate
          : endDate.toISOString();

      if (range !== 'live') {
        setHistoryWindowMs([
          new Date(resolvedStartDate).getTime(),
          new Date(resolvedEndDate).getTime(),
        ]);
      }

      const result = await apiService.getSensorReadings({
        deviceId,
        sensorId: targetSensorId,
        startDate: resolvedStartDate,
        endDate: resolvedEndDate,
        pageSize,
      });

      const readings: ChartDataPoint[] = result.items
        .filter((reading) => reading.sensorId === targetSensorId)
        .map((reading) => ({
          readingId: reading.readingId,
          timestamp: reading.timestamp,
          value: reading.value,
        }));

      const applyRollingWindow = range === 'live' && windowMode === 'time';
      const trimmed = trimChartPoints(
        readings,
        { windowMode, timeWindowMinutes: windowMins, maxDataPoints },
        applyRollingWindow,
        Date.now()
      );

      setChartData(trimmed);
      if (trimmed.length > 0) {
        setCurrentValue(trimmed[trimmed.length - 1].value);
      } else {
        setCurrentValue(null);
      }
    } catch (err: any) {
      console.error('Error fetching readings:', err);
      setError('Failed to load sensor readings');
    } finally {
      if (isInitial) {
        setInitialLoading(false);
      } else {
        setHistoryFetching(false);
      }
    }
  }, [deviceId, windowMode, maxDataPoints, aroundMinutes]);

  const handleNewReading = useCallback(
    (reading: SensorReading) => {
      if (trendRangeRef.current !== 'live') return;
      if (!temperatureSensor) return;
      if (reading.sensorId !== temperatureSensor.sensorId || reading.deviceId !== deviceId) {
        return;
      }
      setCurrentValue(reading.value);
      const now = Date.now();
      setChartData((prev) =>
        trimChartPoints(
          [
            ...prev,
            {
              readingId: reading.readingId,
              timestamp: reading.timestamp,
              value: reading.value,
            },
          ],
          trimOptions,
          true,
          now
        )
      );
    },
    [temperatureSensor, deviceId, trimOptions]
  );

  const applyCenteredWindow = () => {
    if (!centerTimeInput) return;

    const center = new Date(centerTimeInput);
    if (Number.isNaN(center.getTime())) return;

    const start = new Date(center.getTime() - aroundMinutes * 60 * 1000);
    const end = new Date(center.getTime() + aroundMinutes * 60 * 1000);
    setCustomWindow({ startDate: start.toISOString(), endDate: end.toISOString() });
    setHistoryWindowMs([start.getTime(), end.getTime()]);
    setTrendRange('custom');
  };

  // Load sensor metadata when device/sensor changes
  useEffect(() => {
    sensorReadingsReadyRef.current = false;
    setInitialLoading(true);
    setError(null);
    setTemperatureSensor(null);
    setHistoryWindowMs(null);

    const loadSensor = async () => {
      const sensor = await fetchTemperatureSensor();
      if (!sensor) {
        setInitialLoading(false);
      }
    };

    void loadSensor();
  }, [deviceId, sensorId, fetchTemperatureSensor]);

  // Fetch readings when sensor is ready or history range changes
  useEffect(() => {
    if (!temperatureSensor) return;
    if (trendRange === 'custom' && customWindow == null) return;

    const isInitial = !sensorReadingsReadyRef.current;
    sensorReadingsReadyRef.current = true;

    void fetchRecentReadings(temperatureSensor.sensorId, trendRange, timeWindowMinutes, {
      isInitial,
      customWindow: trendRange === 'custom' ? customWindow : null,
      aroundMins: aroundMinutes,
    });
    // timeWindowMinutes: live slider re-trims only (separate effect). aroundMinutes: applied on Show via customWindow.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [temperatureSensor, trendRange, customWindow, fetchRecentReadings]);

  // Re-trim on window change and every live tick so the trace scrolls left as time advances
  useEffect(() => {
    if (trendRange !== 'live' || windowMode !== 'time') return;
    setChartData((prev) => trimChartPoints(prev, trimOptions, true, liveNow));
  }, [liveNow, timeWindowMinutes, trendRange, windowMode, trimOptions]);

  // Set up SignalR for real-time updates
  useEffect(() => {
    if (!temperatureSensor || trendRange !== 'live') return;

    const setupSignalR = async () => {
      try {
        const state = signalRService.getConnectionState();
        if (state !== signalR.HubConnectionState.Connected) {
          await signalRService.start();
        }

        await signalRService.subscribeToSensor(temperatureSensor.sensorId);
        await signalRService.subscribeToDevice(deviceId);
        signalRService.onSensorReading(handleNewReading);
      } catch (err) {
        console.error('Error setting up SignalR:', err);
      }
    };

    setupSignalR();
    return () => {
      signalRService.offSensorReading(handleNewReading);
    };
  }, [temperatureSensor, deviceId, trendRange, handleNewReading]);

  useEffect(() => {
    if (trendRange !== 'live') return;
    const id = window.setInterval(() => setLiveNow(Date.now()), 1000);
    return () => window.clearInterval(id);
  }, [trendRange]);

  const signalKindNormalized = temperatureSensor?.signalKind ?? 'analog';
  const isDiscreteSensor = signalKindNormalized === 'discrete';
  const useAnalogGauge =
    temperatureSensor != null &&
    !isDiscreteSensor &&
    (temperatureSensor.chartStyle ?? 'line') === 'gauge';
  const gaugeDisplayValue =
    currentValue ??
    (chartData.length > 0 ? chartData[chartData.length - 1].value : null);
  const gaugeMin = temperatureSensor?.minValue ?? 0;
  const gaugeMax = temperatureSensor?.maxValue ?? 100;
  /** Plot series: trimmed buffer + one live tip so the line renders with currentValue. */
  const chartDataForPlot = useMemo((): ChartDataPoint[] => {
    if (trendRange !== 'live' || windowMode !== 'time' || currentValue == null) {
      return chartData;
    }
    const buffered = chartData.filter((p) => p.readingId !== -1);
    const last = buffered[buffered.length - 1];
    const lastMs = last ? parseApiTimestampMs(last.timestamp) : 0;
    if (
      last &&
      !Number.isNaN(lastMs) &&
      lastMs >= liveNow - 2000 &&
      last.value === currentValue
    ) {
      return buffered;
    }
    return [
      ...buffered,
      {
        readingId: -1,
        timestamp: new Date(liveNow).toISOString(),
        value: currentValue,
      },
    ];
  }, [chartData, trendRange, windowMode, currentValue, liveNow]);

  const chartYDomain = useMemo((): [number, number] | undefined => {
    if (useAnalogGauge || isDiscreteSensor) return undefined;
    return computeLineChartYDomain(
      chartDataForPlot.map((p) => p.value),
      temperatureSensor?.minValue,
      temperatureSensor?.maxValue
    );
  }, [chartDataForPlot, temperatureSensor, useAnalogGauge, isDiscreteSensor]);

  const liveXDomain = useMemo((): [number, number] | undefined => {
    if (trendRange !== 'live' || windowMode !== 'time') return undefined;
    const windowMs = timeWindowMinutes * 60 * 1000;
    return [liveNow - windowMs, liveNow];
  }, [trendRange, windowMode, timeWindowMinutes, liveNow]);

  const historyXDomain = useMemo((): [number, number] | undefined => {
    if (trendRange === 'live') return undefined;

    if (historyWindowMs != null) return historyWindowMs;

    if (trendRange === 'custom' && customWindow != null) {
      const startMs = new Date(customWindow.startDate).getTime();
      const endMs = new Date(customWindow.endDate).getTime();
      if (Number.isNaN(startMs) || Number.isNaN(endMs)) return undefined;
      return [startMs, endMs];
    }

    const endMs = Date.now();
    const minuteMs = 60 * 1000;
    if (trendRange === '10m') return [endMs - 10 * minuteMs, endMs];
    if (trendRange === '1h') return [endMs - 60 * minuteMs, endMs];
    if (trendRange === '24h') return [endMs - 24 * 60 * minuteMs, endMs];
    return undefined;
  }, [trendRange, customWindow, historyWindowMs]);

  /** Fixed time axis + drop-left once oldest point hits the left edge of the window. */
  const liveScrollActive = useMemo(
    () =>
      trendRange === 'live' &&
      windowMode === 'time' &&
      isLiveScrollActive(chartData, timeWindowMinutes, liveNow),
    [chartData, trendRange, windowMode, timeWindowMinutes, liveNow]
  );

  const isAnalogLineVisualization =
    Boolean(temperatureSensor) && !isDiscreteSensor && !useAnalogGauge;
  const chromeReady = temperatureSensor != null && !error;
  /** Live/10m/24h strip + custom time — analog only (line + gauge), not discrete. */
  const showAnalogHistoryChrome = chromeReady && !isDiscreteSensor;
  /** 2s–10m presets + slider: only for analog line charts in time mode (not discrete; not gauge-only). */
  const showLiveLineWindowTrim =
    chromeReady &&
    isAnalogLineVisualization &&
    windowMode === 'time' &&
    trendRange === 'live';
  const showHistoryRangeHint =
    chromeReady && isAnalogLineVisualization && trendRange !== 'live';

  const setHistoryPreset = (range: '10m' | '1h' | '24h', minutes: number) => {
    const endMs = Date.now();
    setHistoryWindowMs([endMs - minutes * 60 * 1000, endMs]);
    setCustomWindow(null);
    setTrendRange(range);
  };

  const content = (
    <>
      {showPaper && (
        <Typography variant="h6" gutterBottom>
          {deviceName} — {temperatureSensor?.sensorName ?? 'Sensor'}
        </Typography>
      )}
      {showAnalogHistoryChrome ? (
        <>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1, gap: 1, flexWrap: 'wrap' }}>
            <ButtonGroup size="small" variant="outlined">
              <Button
                onClick={() => {
                  setTrendRange('live');
                  setCustomWindow(null);
                  setHistoryWindowMs(null);
                }}
                variant={trendRange === 'live' ? 'contained' : 'outlined'}
              >
                Live
              </Button>
              <Button
                onClick={() => setHistoryPreset('10m', 10)}
                variant={trendRange === '10m' ? 'contained' : 'outlined'}
              >
                10m
              </Button>
              <Button
                onClick={() => setHistoryPreset('1h', 60)}
                variant={trendRange === '1h' ? 'contained' : 'outlined'}
              >
                1h
              </Button>
              <Button
                onClick={() => setHistoryPreset('24h', 24 * 60)}
                variant={trendRange === '24h' ? 'contained' : 'outlined'}
              >
                24h
              </Button>
            </ButtonGroup>
            <Typography variant="caption" color="text.secondary" sx={{ mr: 1 }}>
              {trendRange === 'live'
                ? windowMode === 'time'
                  ? timeWindowMinutes < 1
                    ? `Window: Last ${(timeWindowMinutes * 60).toFixed(0)}s`
                    : `Window: Last ${timeWindowMinutes.toFixed(1)}m`
                  : `Window: Last ${maxDataPoints} points`
                : trendRange === 'custom'
                  ? `Range: centered ±${aroundMinutes}m`
                  : `Range: last ${formatHistoryRangeLabel(trendRange)} ending now`}
            </Typography>
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1, flexWrap: 'wrap' }}>
            <TextField
              size="small"
              type="datetime-local"
              label="At time"
              value={centerTimeInput}
              onChange={(e) => setCenterTimeInput(e.target.value)}
              InputLabelProps={{ shrink: true }}
              helperText="Local time; database stores UTC"
            />
            <TextField
              select
              size="small"
              label="Around"
              value={String(aroundMinutes)}
              onChange={(e) => setAroundMinutes(Number(e.target.value))}
              sx={{ minWidth: 120 }}
            >
              <MenuItem value="1">+/- 1m</MenuItem>
              <MenuItem value="5">+/- 5m</MenuItem>
              <MenuItem value="15">+/- 15m</MenuItem>
              <MenuItem value="30">+/- 30m</MenuItem>
              <MenuItem value="60">+/- 1h</MenuItem>
            </TextField>
            <Button
              size="small"
              variant="outlined"
              onClick={applyCenteredWindow}
              disabled={!centerTimeInput}
            >
              Show
            </Button>
          </Box>
          <Typography variant="caption" color="text.secondary" display="block" sx={{ mb: 1 }}>
            Mode: {trendRange === 'live' ? 'Live updates' : trendRange === 'custom' ? `History (centered +/- ${aroundMinutes}m)` : `History (${trendRange})`}
          </Typography>
          {showHistoryRangeHint ? (
            <Typography variant="caption" color="text.secondary" display="block" sx={{ mb: 1 }}>
              Showing all readings in this history range. Switch to Live for a rolling window.
            </Typography>
          ) : null}
        </>
      ) : null}
      {showPaper && temperatureSensor ? (
        <Typography variant="body2" color="text.secondary" gutterBottom>
          {temperatureSensor.sensorType}
        </Typography>
      ) : null}
      
      {/* Live line chart window trim — presets + slider */}
      {showLiveLineWindowTrim ? (
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
          <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 1 }}>
            {liveScrollActive
              ? `Scrolling window (last ${formatRollingWindowLabel(timeWindowMinutes)}): new points on the right, oldest dropped on the left.`
              : `Filling window (last ${formatRollingWindowLabel(timeWindowMinutes)}): points connect on the right until the trace reaches the left edge, then scroll begins.`}
          </Typography>
        </Box>
      ) : null}
      {/* Current Value Display (analog numeric summary; discrete uses indicator below) */}
      {!initialLoading &&
        !error &&
        temperatureSensor &&
        currentValue !== null &&
        !isDiscreteSensor &&
        (
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
              ? new Date(parseApiTimestampMs(chartData[chartData.length - 1].timestamp)).toLocaleString()
              : '—'}
          </Typography>
        </Box>
      )}
      {initialLoading ? (
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
      ) : isDiscreteSensor ? (
        <Box
          sx={{
            border: 1,
            borderColor: 'divider',
            borderRadius: 2,
            bgcolor: 'background.paper',
            py: 1,
          }}
        >
          <DiscreteSensorIndicator
            value={currentValue}
            sensorName={temperatureSensor.sensorName}
            hideSensorHeading={hideDiscreteSensorHeading}
          />
        </Box>
      ) : chartData.length === 0 &&
        !(trendRange === 'live' && currentValue !== null && !useAnalogGauge) &&
        !(trendRange === 'live' && useAnalogGauge && gaugeDisplayValue !== null) ? (
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
      ) : useAnalogGauge ? (
        <Box
          sx={{
            p: { xs: 1, sm: 2 },
            border: 1,
            borderColor: 'divider',
            borderRadius: 2,
            bgcolor: 'background.paper',
          }}
        >
          <SensorAnalogGauge
            value={gaugeDisplayValue}
            min={Number(gaugeMin)}
            max={Number(gaugeMax)}
            unit={temperatureSensor.unit || ''}
            name={temperatureSensor.sensorName}
          />
        </Box>
      ) : (
        <Box
          sx={{
            position: 'relative',
            p: { xs: 1, sm: 2 },
            border: 1,
            borderColor: 'divider',
            borderRadius: 2,
            bgcolor: 'background.paper',
            opacity: historyFetching ? 0.65 : 1,
            transition: 'opacity 0.15s ease',
          }}
        >
          {historyFetching ? (
            <CircularProgress
              size={22}
              thickness={4}
              sx={{ position: 'absolute', top: 10, right: 10, zIndex: 1 }}
            />
          ) : null}
          <RealTimeChart
            data={chartDataForPlot}
            maxDataPoints={trendRange === 'live' ? maxDataPoints : HISTORY_PAGE_SIZE_CAP}
            isLive={trendRange === 'live'}
            referenceNow={trendRange === 'live' ? liveNow : undefined}
            xDomain={liveScrollActive ? liveXDomain : historyXDomain}
            yDomain={chartYDomain}
            name={temperatureSensor?.sensorName ?? 'Sensor'}
            height={height}
            unit={temperatureSensor.unit || ''}
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

