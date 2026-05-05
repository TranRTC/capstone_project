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
import ActuatorForm, { ActuatorFormValues } from '../components/actuator/ActuatorForm';
import { Actuator, CreateActuator, DeviceList, Sensor } from '../types';

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

function toCreatePayload(v: ActuatorFormValues): CreateActuator {
  return {
    name: v.name.trim(),
    description: v.description?.trim() || undefined,
    kind: v.kind,
    channel: v.channel?.trim() || undefined,
    analogMin: v.analogMin,
    analogMax: v.analogMax,
    controlUnit: v.controlUnit?.trim() || undefined,
    feedbackSensorId: v.feedbackSensorId,
  };
}

function feedbackLabel(actuator: Actuator, sensors: Sensor[]): string {
  if (!actuator.feedbackSensorId) return 'None';
  const s = sensors.find((x) => x.sensorId === actuator.feedbackSensorId);
  return s ? `${s.sensorName} (${s.sensorId})` : `Sensor ID ${actuator.feedbackSensorId}`;
}

const ActuatorsPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const [devices, setDevices] = useState<DeviceList[]>([]);
  const [devicesLoading, setDevicesLoading] = useState(true);
  const [devicesError, setDevicesError] = useState<string | null>(null);
  const [selectedDeviceId, setSelectedDeviceId] = useState<number | ''>('');
  const [actuators, setActuators] = useState<Actuator[]>([]);
  const [actuatorsLoading, setActuatorsLoading] = useState(false);
  const [actuatorsError, setActuatorsError] = useState<string | null>(null);
  const [sensors, setSensors] = useState<Sensor[]>([]);
  const [formOpen, setFormOpen] = useState(false);
  const [editingActuator, setEditingActuator] = useState<Actuator | null>(null);

  useEffect(() => {
    void loadDevices();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

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
      setActuators([]);
      setSensors([]);
      setActuatorsError(null);
      return;
    }
    void loadActuatorsAndSensors(selectedDeviceId);
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

  const loadActuatorsAndSensors = async (deviceId: number) => {
    try {
      setActuatorsLoading(true);
      setActuatorsError(null);
      const [actuatorList, sensorList] = await Promise.all([
        apiService.getActuatorsByDevice(deviceId),
        apiService.getSensorsByDevice(deviceId),
      ]);
      setActuators(actuatorList);
      setSensors(sensorList);
    } catch (err: any) {
      console.error('Error loading actuators/sensors:', err);
      setActuators([]);
      setSensors([]);
      setActuatorsError(err?.message || 'Failed to load actuators');
    } finally {
      setActuatorsLoading(false);
    }
  };

  const handleCloseForm = () => {
    setFormOpen(false);
    setEditingActuator(null);
  };

  const handleSubmitActuator = async (values: ActuatorFormValues) => {
    const deviceId = selectedDeviceId;
    if (deviceId === '') {
      throw new Error('Select a device first.');
    }

    if (editingActuator) {
      await apiService.updateActuator(deviceId, editingActuator.actuatorId, {
        ...toCreatePayload(values),
        isActive: values.isActive,
      });
    } else {
      await apiService.createActuator(deviceId, toCreatePayload(values));
    }
    await loadActuatorsAndSensors(deviceId);
  };

  const handleEditActuator = (actuator: Actuator) => {
    setEditingActuator(actuator);
    setFormOpen(true);
  };

  const handleDeleteActuator = async (actuatorId: number) => {
    if (!window.confirm('Delete this actuator? This cannot be undone.')) return;
    try {
      if (selectedDeviceId === '') return;
      await apiService.deleteActuator(selectedDeviceId, actuatorId);
      await loadActuatorsAndSensors(selectedDeviceId);
    } catch (err) {
      console.error('Error deleting actuator:', err);
      alert('Failed to delete actuator');
    }
  };

  const openCreateForm = () => {
    setEditingActuator(null);
    setFormOpen(true);
  };

  const canManageActuators = selectedDeviceId !== '' && !devicesLoading && devices.length > 0;

  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom sx={{ mb: 2 }}>
        Actuators
      </Typography>

      <ActuatorForm
        key={editingActuator ? `edit-${editingActuator.actuatorId}` : 'create'}
        open={formOpen}
        onClose={handleCloseForm}
        onSubmit={handleSubmitActuator}
        initialData={editingActuator ?? undefined}
        sensors={sensors}
        title={editingActuator ? 'Edit Actuator' : 'Add Actuator'}
        isEdit={Boolean(editingActuator)}
      />

      {devicesError && (
        <MuiAlert
          severity="error"
          sx={{ mb: 3 }}
          action={(
            <Button color="inherit" size="small" onClick={() => void loadDevices()}>
              Retry
            </Button>
          )}
        >
          {devicesError}
        </MuiAlert>
      )}

      <Paper sx={{ ...panelSx, mb: 3 }}>
        <Typography variant="subtitle2" color="text.secondary" sx={{ mb: 1.5 }}>
          Select a device to view and manage its actuators.
        </Typography>
        {devicesLoading ? (
          <Skeleton variant="rounded" width="100%" height={56} sx={{ maxWidth: 400 }} />
        ) : (
          <FormControl fullWidth sx={{ maxWidth: 400 }} disabled={devices.length === 0}>
            <InputLabel id="actuators-device-select-label">Device</InputLabel>
            <Select
              labelId="actuators-device-select-label"
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
                    Managing actuators for device ID {selectedDeviceId}.{' '}
                    <Link to={`/devices/${selectedDeviceId}`}>Open device</Link> to run commands and controls.
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
            disabled={!canManageActuators || actuatorsLoading}
          >
            Add Actuator
          </Button>
        </Box>
      </Paper>

      {actuatorsError && selectedDeviceId !== '' && (
        <MuiAlert
          severity="error"
          sx={{ mb: 3 }}
          action={(
            <Button
              color="inherit"
              size="small"
              onClick={() => void loadActuatorsAndSensors(selectedDeviceId as number)}
            >
              Retry
            </Button>
          )}
        >
          {actuatorsError}
        </MuiAlert>
      )}

      {selectedDeviceId !== '' && actuatorsLoading ? (
        <Paper sx={{ ...panelSx }}>
          <Grid container spacing={2}>
            {[1, 2, 3].map((k) => (
              <Grid item xs={12} sm={6} md={4} key={k}>
                <Skeleton variant="rounded" height={160} />
              </Grid>
            ))}
          </Grid>
        </Paper>
      ) : selectedDeviceId !== '' && actuators.length === 0 && !actuatorsError ? (
        <Paper sx={{ ...panelSx }}>
          <Typography color="text.secondary">No actuators for this device yet.</Typography>
          <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.5 }}>
            Use Add Actuator to register one.
          </Typography>
        </Paper>
      ) : selectedDeviceId !== '' && actuators.length > 0 ? (
        <Grid container spacing={3}>
          {actuators.map((actuator) => (
            <Grid item xs={12} sm={6} md={4} key={actuator.actuatorId}>
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
                      {actuator.name}
                    </Typography>
                    <Chip
                      label={actuator.isActive ? 'Active' : 'Inactive'}
                      color={actuator.isActive ? 'success' : 'default'}
                      size="small"
                    />
                  </Box>
                  <Typography variant="body2" color="text.secondary" gutterBottom>
                    Kind: {actuator.kind}
                  </Typography>
                  {actuator.channel ? (
                    <Typography variant="body2" color="text.secondary">
                      Channel: {actuator.channel}
                    </Typography>
                  ) : null}
                  {actuator.kind === 'Analog' &&
                  actuator.analogMin != null &&
                  actuator.analogMax != null ? (
                    <Typography variant="body2" color="text.secondary">
                      Range: {actuator.analogMin} – {actuator.analogMax}
                      {actuator.controlUnit ? ` ${actuator.controlUnit}` : ''}
                    </Typography>
                    ) : null}
                  <Typography variant="body2" color="text.secondary">
                    Feedback: {feedbackLabel(actuator, sensors)}
                  </Typography>
                  {actuator.description ? (
                    <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.5 }}>
                      {actuator.description}
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
                      onClick={() => handleEditActuator(actuator)}
                      sx={cardActionGhostSageSx}
                    >
                      Edit
                    </Button>
                    <Button
                      size="small"
                      variant="outlined"
                      color="error"
                      onClick={() => handleDeleteActuator(actuator.actuatorId)}
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
      ) : null}
    </Box>
  );
};

export default ActuatorsPage;
