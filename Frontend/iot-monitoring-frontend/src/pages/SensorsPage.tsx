import React, { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import {
  Box,
  Typography,
  Paper,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  FormHelperText,
  Grid,
  Card,
  CardContent,
  Button,
  Chip,
  Skeleton,
  Alert as MuiAlert,
} from '@mui/material';
import { alpha } from '@mui/material/styles';
import { Add as AddIcon } from '@mui/icons-material';
import { apiService } from '../services/api';
import SensorForm, { SensorFormValues } from '../components/sensor/SensorForm';
import DeviceTemperatureChart from '../components/charts/DeviceTemperatureChart';
import { DeviceList, Sensor } from '../types';

const panelSx = {
  p: 2.5,
  border: 1,
  borderColor: 'divider',
  boxShadow: 'none',
  bgcolor: 'background.paper',
};

const cardActionGhostBaseSx = {
  minWidth: 48,
  px: 1.125,
  py: 0.45,
  borderRadius: '999px',
  fontSize: '0.8125rem',
  fontWeight: 500,
  textTransform: 'none' as const,
  lineHeight: 1.2,
  boxShadow: 'none',
  bgcolor: 'transparent',
  borderWidth: 1,
  borderStyle: 'solid',
};

const cardActionGhostSageSx = {
  ...cardActionGhostBaseSx,
  borderColor: '#8fad93',
  color: '#1e4d3a',
  '&:hover': {
    boxShadow: 'none',
    borderColor: '#6f9276',
    bgcolor: alpha('#8fad93', 0.12),
  },
};

const cardActionGhostDangerSx = {
  ...cardActionGhostBaseSx,
  borderColor: alpha('#b71c1c', 0.45),
  color: '#8b1a1a',
  '&:hover': {
    boxShadow: 'none',
    borderColor: alpha('#b71c1c', 0.65),
    bgcolor: alpha('#b71c1c', 0.06),
  },
};

function sensorToFormValues(s: Sensor): SensorFormValues {
  return {
    sensorName: s.sensorName,
    sensorType: s.sensorType,
    unit: s.unit ?? '',
    edgeDeviceId: s.edgeDeviceId ?? '',
    minValue: s.minValue ?? undefined,
    maxValue: s.maxValue ?? undefined,
    isActive: s.isActive,
  };
}

function toCreatePayload(v: SensorFormValues) {
  return {
    sensorName: v.sensorName.trim(),
    sensorType: v.sensorType.trim(),
    unit: v.unit?.trim() || undefined,
    edgeDeviceId: v.edgeDeviceId?.trim() || undefined,
    minValue: v.minValue,
    maxValue: v.maxValue,
  };
}

const SensorsPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const [devices, setDevices] = useState<DeviceList[]>([]);
  const [devicesLoading, setDevicesLoading] = useState(true);
  const [devicesError, setDevicesError] = useState<string | null>(null);
  const [selectedDeviceId, setSelectedDeviceId] = useState<number | ''>('');
  const [sensors, setSensors] = useState<Sensor[]>([]);
  const [sensorsLoading, setSensorsLoading] = useState(false);
  const [sensorsError, setSensorsError] = useState<string | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [editingSensor, setEditingSensor] = useState<Sensor | null>(null);
  const [chartSensorId, setChartSensorId] = useState<number | ''>('');

  useEffect(() => {
    loadDevices();
  }, []);

  useEffect(() => {
    if (sensors.length === 0) {
      setChartSensorId('');
      return;
    }

    const qRaw = searchParams.get('sensorId');
    const qSid = qRaw != null ? Number.parseInt(qRaw, 10) : NaN;
    if (Number.isFinite(qSid) && sensors.some((s) => s.sensorId === qSid)) {
      setChartSensorId(qSid);
      return;
    }

    setChartSensorId((prev) => {
      if (typeof prev === 'number' && sensors.some((s) => s.sensorId === prev)) {
        return prev;
      }
      const defaultSensor =
        sensors.find(
          (s) =>
            s.sensorType.toLowerCase().includes('temperature') ||
            s.sensorType.toLowerCase().includes('temp')
        ) ?? sensors[0];
      return defaultSensor.sensorId;
    });
  }, [sensors, searchParams]);

  useEffect(() => {
    if (devicesLoading || devices.length === 0) return;
    const qRaw = searchParams.get('deviceId');
    const qId = qRaw != null ? Number.parseInt(qRaw, 10) : NaN;
    if (Number.isFinite(qId) && devices.some((d) => d.deviceId === qId)) {
      setSelectedDeviceId(qId);
    }
  }, [searchParams, devices, devicesLoading]);

  useEffect(() => {
    if (selectedDeviceId === '') {
      setSensors([]);
      setSensorsError(null);
      return;
    }
    loadSensors(selectedDeviceId);
  }, [selectedDeviceId]);

  const loadDevices = async () => {
    try {
      setDevicesLoading(true);
      setDevicesError(null);
      const data = await apiService.getDevices();
      setDevices(data);
      if (data.length === 0) {
        setSelectedDeviceId('');
      } else {
        const qRaw = searchParams.get('deviceId');
        const qId = qRaw != null ? Number.parseInt(qRaw, 10) : NaN;
        if (Number.isFinite(qId) && data.some((d) => d.deviceId === qId)) {
          setSelectedDeviceId(qId);
        } else {
          setSelectedDeviceId((prev) => (prev === '' ? data[0].deviceId : prev));
        }
      }
    } catch (err: any) {
      console.error('Error loading devices:', err);
      setDevices([]);
      setDevicesError(err?.message || 'Failed to load devices');
    } finally {
      setDevicesLoading(false);
    }
  };

  const loadSensors = async (deviceId: number) => {
    try {
      setSensorsLoading(true);
      setSensorsError(null);
      const data = await apiService.getSensorsByDevice(deviceId);
      setSensors(data);
    } catch (err: any) {
      console.error('Error loading sensors:', err);
      setSensors([]);
      setSensorsError(err?.message || 'Failed to load sensors');
    } finally {
      setSensorsLoading(false);
    }
  };

  const handleCloseForm = () => {
    setFormOpen(false);
    setEditingSensor(null);
  };

  const handleSubmitSensor = async (values: SensorFormValues) => {
    const deviceId = selectedDeviceId;
    if (deviceId === '') {
      throw new Error('Select a device first.');
    }

    if (editingSensor) {
      await apiService.updateSensor(editingSensor.sensorId, {
        ...toCreatePayload(values),
        isActive: values.isActive,
      });
    } else {
      await apiService.createSensor(deviceId, toCreatePayload(values));
    }
    await loadSensors(deviceId);
  };

  const handleEditSensor = (sensor: Sensor) => {
    setEditingSensor(sensor);
    setFormOpen(true);
  };

  const handleDeleteSensor = async (sensorId: number) => {
    if (!window.confirm('Delete this sensor? This cannot be undone.')) return;
    try {
      await apiService.deleteSensor(sensorId);
      if (selectedDeviceId !== '') {
        await loadSensors(selectedDeviceId);
      }
    } catch (err) {
      console.error('Error deleting sensor:', err);
      alert('Failed to delete sensor');
    }
  };

  const openCreateForm = () => {
    setEditingSensor(null);
    setFormOpen(true);
  };

  const canManageSensors = selectedDeviceId !== '' && !devicesLoading && devices.length > 0;

  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom sx={{ mb: 2 }}>
        Sensors
      </Typography>

      <SensorForm
        key={editingSensor ? `edit-${editingSensor.sensorId}` : 'create'}
        open={formOpen}
        onClose={handleCloseForm}
        onSubmit={handleSubmitSensor}
        initialData={editingSensor ? sensorToFormValues(editingSensor) : undefined}
        title={editingSensor ? 'Edit Sensor' : 'Add Sensor'}
        isEdit={Boolean(editingSensor)}
      />

      {devicesError && (
        <MuiAlert
          severity="error"
          sx={{ mb: 3 }}
          action={(
            <Button color="inherit" size="small" onClick={loadDevices}>
              Retry
            </Button>
          )}
        >
          {devicesError}
        </MuiAlert>
      )}

      <Paper sx={{ ...panelSx, mb: 3 }}>
        <Typography variant="subtitle2" color="text.secondary" sx={{ mb: 1.5 }}>
          Select a device to view and manage its sensors.
        </Typography>
        {devicesLoading ? (
          <Skeleton variant="rounded" width="100%" height={56} sx={{ maxWidth: 400 }} />
        ) : (
          <FormControl fullWidth sx={{ maxWidth: 400 }} disabled={devices.length === 0}>
            <InputLabel id="sensors-device-select-label">Device</InputLabel>
            <Select
              labelId="sensors-device-select-label"
              value={selectedDeviceId}
              label="Device"
              onChange={(e) => setSelectedDeviceId(e.target.value as number)}
            >
              {devices.map((device) => (
                <MenuItem key={device.deviceId} value={device.deviceId}>
                  {device.deviceName} ({device.deviceType})
                </MenuItem>
              ))}
            </Select>
            {devices.length === 0 ? (
              <FormHelperText>
                No devices yet.{' '}
                <Link to="/devices">Register a device</Link> first.
              </FormHelperText>
            ) : (
              <FormHelperText>
                {selectedDeviceId !== '' && (
                  <>
                    Managing sensors for device ID {selectedDeviceId}.{' '}
                    <Link to={`/devices/${selectedDeviceId}`}>Open device</Link>
                  </>
                )}
              </FormHelperText>
            )}
          </FormControl>
        )}

        <Box sx={{ mt: 2, display: 'flex', flexWrap: 'wrap', gap: 1, alignItems: 'center' }}>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={openCreateForm}
            disabled={!canManageSensors || sensorsLoading}
          >
            Add Sensor
          </Button>
        </Box>
      </Paper>

      {sensorsError && selectedDeviceId !== '' && (
        <MuiAlert
          severity="error"
          sx={{ mb: 3 }}
          action={(
            <Button
              color="inherit"
              size="small"
              onClick={() => loadSensors(selectedDeviceId as number)}
            >
              Retry
            </Button>
          )}
        >
          {sensorsError}
        </MuiAlert>
      )}

      {selectedDeviceId !== '' && sensorsLoading ? (
        <Paper sx={{ ...panelSx }}>
          <Grid container spacing={2}>
            {[1, 2, 3].map((k) => (
              <Grid item xs={12} sm={6} md={4} key={k}>
                <Skeleton variant="rounded" height={160} />
              </Grid>
            ))}
          </Grid>
        </Paper>
      ) : selectedDeviceId !== '' && sensors.length === 0 && !sensorsError ? (
        <Paper sx={{ ...panelSx }}>
          <Typography color="text.secondary">No sensors for this device yet.</Typography>
          <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.5 }}>
            Use Add Sensor to register one.
          </Typography>
        </Paper>
      ) : selectedDeviceId !== '' && sensors.length > 0 ? (
        <>
          <Grid container spacing={3}>
            {sensors.map((sensor) => (
              <Grid item xs={12} sm={6} md={4} key={sensor.sensorId}>
                <Card
                  variant="outlined"
                  sx={{
                    height: '100%',
                    boxShadow: 'none',
                    borderColor: 'divider',
                    display: 'flex',
                    flexDirection: 'column',
                  }}
                >
                  <CardContent sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column' }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 1, mb: 1 }}>
                      <Typography variant="h6" component="h2" sx={{ fontSize: '1.05rem' }}>
                        {sensor.sensorName}
                      </Typography>
                      <Chip
                        label={sensor.isActive ? 'Active' : 'Inactive'}
                        color={sensor.isActive ? 'success' : 'default'}
                        size="small"
                      />
                    </Box>
                    <Typography variant="body2" color="text.secondary" gutterBottom>
                      Type: {sensor.sensorType}
                    </Typography>
                    {sensor.unit ? (
                      <Typography variant="body2" color="text.secondary">
                        Unit: {sensor.unit}
                      </Typography>
                    ) : null}
                    {sensor.minValue != null && sensor.maxValue != null ? (
                      <Typography variant="body2" color="text.secondary">
                        Range: {sensor.minValue} – {sensor.maxValue}
                        {sensor.unit ? ` ${sensor.unit}` : ''}
                      </Typography>
                    ) : null}
                    {sensor.edgeDeviceId ? (
                      <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.5 }}>
                        Edge ID: {sensor.edgeDeviceId}
                      </Typography>
                    ) : null}
                    <Box
                      sx={{
                        mt: 'auto',
                        pt: 2,
                        display: 'flex',
                        flexWrap: 'wrap',
                        gap: 0.5,
                      }}
                    >
                      <Button
                        size="small"
                        variant="outlined"
                        color="success"
                        onClick={() => handleEditSensor(sensor)}
                        sx={cardActionGhostSageSx}
                      >
                        Edit
                      </Button>
                      <Button
                        size="small"
                        variant="outlined"
                        color="error"
                        onClick={() => handleDeleteSensor(sensor.sensorId)}
                        sx={cardActionGhostDangerSx}
                      >
                        Delete
                      </Button>
                    </Box>
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>

          <Paper sx={{ ...panelSx, mt: 3 }}>
            <Typography variant="h5" component="h2" gutterBottom>
              Sensor readings
            </Typography>
            <Typography variant="caption" color="text.secondary" display="block" sx={{ mb: 1.5 }}>
              Choose which sensor to plot. Readings and live updates apply to that sensor only.
            </Typography>
            <FormControl fullWidth sx={{ mb: 2, maxWidth: 480 }}>
              <InputLabel id="chart-sensor-select-label">Chart sensor</InputLabel>
              <Select
                labelId="chart-sensor-select-label"
                value={chartSensorId}
                label="Chart sensor"
                onChange={(e) => setChartSensorId(e.target.value as number)}
              >
                {sensors.map((sensor) => (
                  <MenuItem key={sensor.sensorId} value={sensor.sensorId}>
                    {sensor.sensorName} ({sensor.sensorType}) — ID {sensor.sensorId}
                  </MenuItem>
                ))}
              </Select>
              <FormHelperText>
                Optional URL: add <code>?sensorId=…</code> together with <code>?deviceId=…</code> to open this chart directly.
              </FormHelperText>
            </FormControl>
            {chartSensorId !== '' ? (
              <DeviceTemperatureChart
                key={chartSensorId}
                deviceId={selectedDeviceId as number}
                deviceName={
                  devices.find((d) => d.deviceId === selectedDeviceId)?.deviceName
                  ?? `Device ${selectedDeviceId}`
                }
                sensorId={chartSensorId as number}
                height={500}
                showPaper={false}
                windowMode="time"
                timeWindowMinutes={5}
              />
            ) : (
              <Typography color="text.secondary">Select a sensor to show the chart.</Typography>
            )}
          </Paper>
        </>
      ) : null}
    </Box>
  );
};

export default SensorsPage;
