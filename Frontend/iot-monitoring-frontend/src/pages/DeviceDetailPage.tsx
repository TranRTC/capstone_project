import React, { useEffect, useState } from 'react';
import { useParams, useNavigate, Link as RouterLink } from 'react-router-dom';
import {
  Box,
  Typography,
  Paper,
  Grid,
  Button,
  Chip,
  Skeleton,
  Breadcrumbs,
  Link,
  Alert as MuiAlert,
  Stack,
  FormControlLabel,
  Switch,
  Slider,
  TextField,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  IconButton,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline';
import { apiService } from '../services/api';
import DeviceTemperatureChart from '../components/charts/DeviceTemperatureChart';
import SensorForm, { SensorFormValues } from '../components/sensor/SensorForm';
import {
  Device,
  DeviceCommand,
  DeviceConfiguration,
  Actuator,
  Sensor,
  CreateActuator,
  CreateSensor,
} from '../types';

const panelSx = {
  p: 2.5,
  border: 1,
  borderColor: 'divider',
  boxShadow: 'none',
  bgcolor: 'background.paper',
};

/** Aligns section title rows (device / sensors / actuators). */
const sectionHeaderSx = {
  display: 'flex',
  justifyContent: 'space-between',
  alignItems: 'center',
  mb: 2,
  flexWrap: 'wrap',
  gap: 1,
  minHeight: 36,
};

/** Uniform Active/Inactive chip (device header + table Status column). */
const statusChipSx = {
  height: 24,
  minWidth: 72,
  '& .MuiChip-label': { px: 1.25, lineHeight: 1.25 },
};

/** Vertically center cell content when name/kind columns wrap to multiple lines. */
const tableAlignSx = {
  minWidth: 560,
  '& .MuiTableCell-root': {
    verticalAlign: 'middle',
  },
  '& .MuiTableCell-head': {
    verticalAlign: 'bottom',
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

function toSensorCreatePayload(v: SensorFormValues): CreateSensor {
  return {
    sensorName: v.sensorName.trim(),
    sensorType: v.sensorType.trim(),
    unit: v.unit?.trim() || undefined,
    edgeDeviceId: v.edgeDeviceId?.trim() || undefined,
    minValue: v.minValue,
    maxValue: v.maxValue,
  };
}

const DeviceDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [device, setDevice] = useState<Device | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [latestCommand, setLatestCommand] = useState<DeviceCommand | null>(null);
  const [commandLoading, setCommandLoading] = useState(false);
  const [commandError, setCommandError] = useState<string | null>(null);
  const [commandSuccess, setCommandSuccess] = useState<string | null>(null);
  const [powerTarget, setPowerTarget] = useState(false);
  const [analogTarget, setAnalogTarget] = useState(50);
  const [capabilityDraft, setCapabilityDraft] = useState({
    supportsTelemetry: true,
    supportsPowerControl: false,
    supportsAnalogControl: false,
    analogMin: 0,
    analogMax: 100,
    analogStep: 1,
    controlUnit: '',
  });

  const [actuators, setActuators] = useState<Actuator[]>([]);
  const [sensors, setSensors] = useState<Sensor[]>([]);
  const [feedbackBySensorId, setFeedbackBySensorId] = useState<Record<number, number | undefined>>({});
  const [powerByActuatorId, setPowerByActuatorId] = useState<Record<number, boolean>>({});
  const [analogByActuatorId, setAnalogByActuatorId] = useState<Record<number, number>>({});
  const [actuatorDialogOpen, setActuatorDialogOpen] = useState(false);
  const [actuatorEditingId, setActuatorEditingId] = useState<number | null>(null);
  const [actuatorForm, setActuatorForm] = useState<CreateActuator>({
    name: '',
    description: '',
    kind: 'Discrete',
    channel: '',
    analogMin: undefined,
    analogMax: undefined,
    controlUnit: '',
    feedbackSensorId: undefined,
  });
  const [actuatorSaving, setActuatorSaving] = useState(false);
  /** Live chart dialog for one sensor */
  const [sensorChartSensorId, setSensorChartSensorId] = useState<number | null>(null);
  /** Control + feedback dialog for one actuator */
  const [actuatorControlTarget, setActuatorControlTarget] = useState<Actuator | null>(null);
  const [sensorFormOpen, setSensorFormOpen] = useState(false);
  const [editingSensor, setEditingSensor] = useState<Sensor | null>(null);
  const [feedbackLinkSavingId, setFeedbackLinkSavingId] = useState<number | null>(null);

  const buildCapabilityModel = (configs: DeviceConfiguration[], deviceType?: string) => {
    const lowered = (deviceType ?? '').toLowerCase();
    const fallbackActuatorLike =
      lowered.includes('motor') || lowered.includes('actuator') || lowered.includes('controller');
    const map = new Map<string, string>();
    configs.forEach((c) => map.set(c.configurationKey, c.configurationValue ?? ''));

    const parseBool = (key: string, fallback: boolean) => {
      const value = map.get(key);
      return value == null || value === '' ? fallback : value.toLowerCase() === 'true';
    };
    const parseNumber = (key: string, fallback: number) => {
      const value = map.get(key);
      const num = Number(value);
      return Number.isFinite(num) ? num : fallback;
    };

    return {
      supportsTelemetry: parseBool('supportsTelemetry', true),
      supportsPowerControl: parseBool('supportsPowerControl', fallbackActuatorLike),
      supportsAnalogControl: parseBool('supportsAnalogControl', fallbackActuatorLike),
      analogMin: parseNumber('analogMin', 0),
      analogMax: parseNumber('analogMax', 100),
      analogStep: parseNumber('analogStep', 1),
      controlUnit: map.get('controlUnit') || '',
    };
  };

  // loadDevice is intentionally excluded to keep navigation-driven loading simple for this page.
  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(() => {
    if (!id) {
      setDevice(null);
      setError('Invalid device link.');
      setLoading(false);
      return;
    }
    const deviceId = Number.parseInt(id, 10);
    if (!Number.isFinite(deviceId)) {
      setDevice(null);
      setError('Invalid device ID.');
      setLoading(false);
      return;
    }
    loadDevice(deviceId);
  }, [id]);

  const refreshFeedbackForActuators = async (list: Actuator[]) => {
    const ids = Array.from(
      new Set(list.map((a) => a.feedbackSensorId).filter((x): x is number => x != null && x > 0))
    );
    if (ids.length === 0) {
      setFeedbackBySensorId({});
      return;
    }
    const next: Record<number, number | undefined> = {};
    await Promise.all(
      ids.map(async (sensorId) => {
        try {
          const page = await apiService.getSensorReadings({
            sensorId,
            pageNumber: 1,
            pageSize: 1,
          });
          next[sensorId] = page.items[0]?.value;
        } catch {
          next[sensorId] = undefined;
        }
      })
    );
    setFeedbackBySensorId(next);
  };

  const loadDevice = async (deviceId: number) => {
    try {
      setLoading(true);
      setError(null);
      const [deviceData, configs, sensorList, actuatorList] = await Promise.all([
        apiService.getDevice(deviceId),
        apiService.getDeviceConfigurations(deviceId),
        apiService.getSensorsByDevice(deviceId),
        apiService.getActuatorsByDevice(deviceId),
      ]);
      const capabilities = buildCapabilityModel(configs, deviceData.deviceType);
      setDevice(deviceData);
      setSensors(sensorList);
      setActuators(actuatorList);
      setAnalogTarget(capabilities.analogMin);
      setCapabilityDraft(capabilities);

      const powerInit: Record<number, boolean> = {};
      const analogInit: Record<number, number> = {};
      actuatorList.forEach((a) => {
        powerInit[a.actuatorId] = false;
        analogInit[a.actuatorId] =
          a.kind.toLowerCase() === 'analog' ? (a.analogMin ?? 0) : 0;
      });
      setPowerByActuatorId(powerInit);
      setAnalogByActuatorId(analogInit);

      await refreshFeedbackForActuators(actuatorList);

      if (capabilities.supportsPowerControl || capabilities.supportsAnalogControl) {
        await refreshLatestCommand(deviceData.deviceId);
      } else {
        setLatestCommand(null);
      }
    } catch (err: any) {
      console.error('Error loading device:', err);
      setDevice(null);
      setActuators([]);
      setSensors([]);
      setError(err.message || 'Failed to load device');
    } finally {
      setLoading(false);
    }
  };

  const refreshLatestCommand = async (deviceId: number) => {
    try {
      const commands = await apiService.getDeviceCommands(deviceId, { pageNumber: 1, pageSize: 1 });
      setLatestCommand(commands.items.length > 0 ? commands.items[0] : null);
    } catch {
      // non-blocking for device details page
    }
  };

  const submitPowerCommand = async (on: boolean) => {
    if (!device) return;
    try {
      setCommandLoading(true);
      setCommandError(null);
      setCommandSuccess(null);
      const command = await apiService.createDeviceCommand(device.deviceId, {
        commandType: 'SetPower',
        payload: JSON.stringify({ on }),
        correlationId: `ui-power-${Date.now()}`,
      });
      setLatestCommand(command);
      setCommandSuccess(`Power command submitted (${command.status}).`);
    } catch (err: any) {
      setCommandError(err.message || 'Failed to submit power command.');
      setPowerTarget((prev) => !prev);
    } finally {
      setCommandLoading(false);
    }
  };

  const submitAnalogCommand = async () => {
    if (!device) return;
    try {
      setCommandLoading(true);
      setCommandError(null);
      setCommandSuccess(null);
      const command = await apiService.createDeviceCommand(device.deviceId, {
        commandType: 'SetValue',
        payload: JSON.stringify({ value: analogTarget }),
        correlationId: `ui-value-${Date.now()}`,
      });
      setLatestCommand(command);
      setCommandSuccess(`SetValue command submitted (${command.status}).`);
    } catch (err: any) {
      setCommandError(err.message || 'Failed to submit analog command.');
    } finally {
      setCommandLoading(false);
    }
  };

  const submitActuatorPower = async (actuator: Actuator, on: boolean) => {
    if (!device) return;
    try {
      setCommandLoading(true);
      setCommandError(null);
      setCommandSuccess(null);
      const command = await apiService.createDeviceCommand(device.deviceId, {
        commandType: 'SetPower',
        payload: JSON.stringify({ on }),
        actuatorId: actuator.actuatorId,
        correlationId: `ui-act-${actuator.actuatorId}-pwr-${Date.now()}`,
      });
      setLatestCommand(command);
      setPowerByActuatorId((p) => ({ ...p, [actuator.actuatorId]: on }));
      setCommandSuccess(`Command sent to "${actuator.name}" (${command.status}).`);
      await refreshFeedbackForActuators(actuators);
    } catch (err: any) {
      setCommandError(err.message || 'Failed to send command.');
      setPowerByActuatorId((p) => ({ ...p, [actuator.actuatorId]: !on }));
    } finally {
      setCommandLoading(false);
    }
  };

  const submitActuatorValue = async (actuator: Actuator) => {
    if (!device) return;
    const value = analogByActuatorId[actuator.actuatorId] ?? 0;
    try {
      setCommandLoading(true);
      setCommandError(null);
      setCommandSuccess(null);
      const command = await apiService.createDeviceCommand(device.deviceId, {
        commandType: 'SetValue',
        payload: JSON.stringify({ value }),
        actuatorId: actuator.actuatorId,
        correlationId: `ui-act-${actuator.actuatorId}-val-${Date.now()}`,
      });
      setLatestCommand(command);
      setCommandSuccess(`SetValue sent to "${actuator.name}" (${command.status}).`);
      await refreshFeedbackForActuators(actuators);
    } catch (err: any) {
      setCommandError(err.message || 'Failed to send command.');
    } finally {
      setCommandLoading(false);
    }
  };

  const openActuatorDialog = (mode: 'add' | 'edit', target?: Actuator) => {
    if (mode === 'add') {
      setActuatorEditingId(null);
      setActuatorForm({
        name: '',
        description: '',
        kind: 'Discrete',
        channel: '',
        analogMin: undefined,
        analogMax: undefined,
        controlUnit: '',
        feedbackSensorId: undefined,
      });
    } else if (target) {
      setActuatorEditingId(target.actuatorId);
      setActuatorForm({
        name: target.name,
        description: target.description ?? '',
        kind: target.kind,
        channel: target.channel ?? '',
        analogMin: target.analogMin,
        analogMax: target.analogMax,
        controlUnit: target.controlUnit ?? '',
        feedbackSensorId: target.feedbackSensorId,
      });
    }
    setActuatorDialogOpen(true);
  };

  const saveActuatorDialog = async () => {
    if (!device) return;
    const name = actuatorForm.name.trim();
    if (!name) {
      setCommandError('Actuator name is required.');
      return;
    }
    try {
      setActuatorSaving(true);
      setCommandError(null);
      const payload: CreateActuator = {
        name,
        description: actuatorForm.description?.trim() || undefined,
        kind: actuatorForm.kind,
        channel: actuatorForm.channel?.trim() || undefined,
        analogMin: actuatorForm.kind === 'Analog' ? actuatorForm.analogMin : undefined,
        analogMax: actuatorForm.kind === 'Analog' ? actuatorForm.analogMax : undefined,
        controlUnit: actuatorForm.kind === 'Analog' ? actuatorForm.controlUnit?.trim() || undefined : undefined,
        feedbackSensorId: actuatorForm.feedbackSensorId || undefined,
      };
      if (actuatorEditingId == null) {
        await apiService.createActuator(device.deviceId, payload);
      } else {
        await apiService.updateActuator(device.deviceId, actuatorEditingId, payload);
      }
      setActuatorDialogOpen(false);
      await loadDevice(device.deviceId);
    } catch (err: any) {
      setCommandError(err.message || 'Failed to save actuator.');
    } finally {
      setActuatorSaving(false);
    }
  };

  const deleteActuator = async (actuator: Actuator) => {
    if (!device) return;
    // eslint-disable-next-line no-alert
    if (!window.confirm(`Delete actuator "${actuator.name}"?`)) return;
    try {
      setCommandError(null);
      await apiService.deleteActuator(device.deviceId, actuator.actuatorId);
      await loadDevice(device.deviceId);
    } catch (err: any) {
      setCommandError(err.message || 'Failed to delete actuator (commands may still reference it).');
    }
  };

  const closeSensorForm = () => {
    setSensorFormOpen(false);
    setEditingSensor(null);
  };

  const handleSubmitSensorForm = async (values: SensorFormValues) => {
    if (!device) return;
    if (editingSensor) {
      await apiService.updateSensor(editingSensor.sensorId, {
        ...toSensorCreatePayload(values),
        isActive: values.isActive,
      });
    } else {
      await apiService.createSensor(device.deviceId, toSensorCreatePayload(values));
    }
    closeSensorForm();
    await loadDevice(device.deviceId);
  };

  const openSensorForm = (mode: 'add' | 'edit', target?: Sensor) => {
    if (mode === 'add') {
      setEditingSensor(null);
    } else if (target) {
      setEditingSensor(target);
    }
    setSensorFormOpen(true);
  };

  const deleteSensorRow = async (s: Sensor) => {
    if (!device) return;
    // eslint-disable-next-line no-alert
    if (!window.confirm(`Delete sensor "${s.sensorName}"? This cannot be undone.`)) return;
    try {
      setCommandError(null);
      await apiService.deleteSensor(s.sensorId);
      if (sensorChartSensorId === s.sensorId) {
        setSensorChartSensorId(null);
      }
      await loadDevice(device.deviceId);
    } catch (err: any) {
      setCommandError(err.message || 'Failed to delete sensor.');
    }
  };

  const sensorLabel = (sensorId?: number) => {
    if (!sensorId) return '';
    const s = sensors.find((x) => x.sensorId === sensorId);
    return s ? `${s.sensorName} (#${sensorId})` : `#${sensorId}`;
  };

  const handleFeedbackSensorChange = async (actuator: Actuator, feedbackSensorId: number | undefined) => {
    if (!device) return;
    try {
      setCommandError(null);
      setFeedbackLinkSavingId(actuator.actuatorId);
      await apiService.updateActuator(device.deviceId, actuator.actuatorId, {
        feedbackSensorId,
      });
      await loadDevice(device.deviceId);
    } catch (err: any) {
      setCommandError(err.message || 'Failed to link feedback sensor.');
    } finally {
      setFeedbackLinkSavingId(null);
    }
  };

  const parsedId = id ? Number.parseInt(id, 10) : NaN;
  const canRetry = Number.isFinite(parsedId);
  const isControllableDevice = capabilityDraft.supportsPowerControl || capabilityDraft.supportsAnalogControl;
  const useLegacyDeviceControl = isControllableDevice && actuators.length === 0;

  if (loading) {
    return (
      <Box>
        <Skeleton variant="text" width={280} height={32} sx={{ mb: 2 }} />
        <Paper sx={{ ...panelSx, mb: 3 }}>
          <Skeleton variant="text" width="60%" height={48} sx={{ mb: 2 }} />
          <Grid container spacing={2}>
            {[1, 2, 3, 4].map((k) => (
              <Grid item xs={12} sm={6} md={3} key={k}>
                <Skeleton variant="rounded" height={88} />
              </Grid>
            ))}
          </Grid>
        </Paper>
      </Box>
    );
  }

  if (error || !device) {
    return (
      <Box>
        <Breadcrumbs sx={{ mb: 2 }}>
          <Link component={RouterLink} to="/devices" color="inherit" underline="hover">
            Devices
          </Link>
          <Typography color="text.primary">Detail</Typography>
        </Breadcrumbs>

        <MuiAlert
          severity="error"
          sx={{ mb: 2 }}
          action={(
            <Button
              color="inherit"
              size="small"
              disabled={!canRetry}
              onClick={() => canRetry && loadDevice(parsedId)}
            >
              Retry
            </Button>
          )}
        >
          {error || 'Device not found'}
        </MuiAlert>

        <Button variant="outlined" onClick={() => navigate('/devices')}>
          Back to Devices
        </Button>
      </Box>
    );
  }

  return (
    <Box>
      <Breadcrumbs sx={{ mb: 2 }}>
        <Link component={RouterLink} to="/devices" color="inherit" underline="hover">
          Devices
        </Link>
        <Typography color="text.primary">{device.deviceName}</Typography>
      </Breadcrumbs>

      <Typography variant="body2" color="text.secondary" sx={{ mb: 2, maxWidth: 720 }}>
        Operator view: each sensor opens a <strong>live chart</strong>; each actuator opens <strong>control</strong> with feedback.
        Edit definitions on the{' '}
        <Link component={RouterLink} to={`/sensors?deviceId=${device.deviceId}`} underline="hover">Sensors</Link>
        {' '}and{' '}
        <Link component={RouterLink} to={`/actuators?deviceId=${device.deviceId}`} underline="hover">Actuators</Link> tabs.
      </Typography>

      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12}>
          <Paper
            sx={{
              ...panelSx,
              p: 1.75,
            }}
          >
            <Box sx={sectionHeaderSx}>
              <Typography variant="h6" component="h1" sx={{ fontWeight: 700, lineHeight: 1.25 }}>
                {device.deviceName}
              </Typography>
              <Chip
                label={device.isActive ? 'Active' : 'Inactive'}
                color={device.isActive ? 'success' : 'default'}
                size="small"
                sx={statusChipSx}
              />
            </Box>

            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: {
                  xs: '1fr 1fr',
                  sm: 'repeat(3, minmax(0, 1fr))',
                  md: 'repeat(4, minmax(0, 1fr))',
                },
                gap: { xs: 1.25, sm: 1.5 },
                columnGap: 2,
                rowGap: 1.25,
              }}
            >
              {[
                { label: 'Device ID', value: String(device.deviceId) },
                { label: 'Device Type', value: device.deviceType },
                ...(device.location ? [{ label: 'Location', value: device.location }] : []),
                ...(device.facilityType ? [{ label: 'Facility Type', value: device.facilityType }] : []),
                ...(device.edgeDeviceType ? [{ label: 'Edge Type', value: device.edgeDeviceType }] : []),
                ...(device.edgeDeviceId ? [{ label: 'Edge ID', value: device.edgeDeviceId }] : []),
                {
                  label: 'Last Seen',
                  value: device.lastSeenAt
                    ? new Date(device.lastSeenAt).toLocaleString()
                    : 'Never',
                },
                {
                  label: 'Created',
                  value: new Date(device.createdAt).toLocaleString(),
                },
              ].map((row) => (
                <Box key={row.label} sx={{ minWidth: 0 }}>
                  <Typography variant="caption" color="text.secondary" display="block" sx={{ lineHeight: 1.2 }}>
                    {row.label}
                  </Typography>
                  <Typography
                    variant="body2"
                    sx={{
                      fontWeight: 600,
                      mt: 0.25,
                      lineHeight: 1.35,
                      wordBreak: 'break-word',
                    }}
                  >
                    {row.value}
                  </Typography>
                </Box>
              ))}
            </Box>
          </Paper>
        </Grid>

        <Grid item xs={12}>
          <Paper sx={panelSx}>
            <Box sx={sectionHeaderSx}>
              <Typography variant="h6" sx={{ lineHeight: 1.25 }}>Sensors (inputs)</Typography>
              <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                <Button component={RouterLink} to={`/sensors?deviceId=${device.deviceId}`} variant="outlined" size="small">
                  Manage sensors
                </Button>
                <Button startIcon={<AddIcon />} variant="contained" size="small" onClick={() => openSensorForm('add')}>
                  Add sensor
                </Button>
              </Stack>
            </Box>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              One row per sensor. <strong>View</strong> opens a live chart with SignalR updates.
            </Typography>
            {sensors.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                No sensors yet.{' '}
                <Link component={RouterLink} to={`/sensors?deviceId=${device.deviceId}`} underline="hover">
                  Add sensors
                </Link>
              </Typography>
            ) : (
              <TableContainer>
                <Table size="small" sx={tableAlignSx}>
                  <TableHead>
                    <TableRow>
                      <TableCell>Sensor</TableCell>
                      <TableCell>Type / unit</TableCell>
                      <TableCell align="center" sx={{ width: 120 }}>Status</TableCell>
                      <TableCell align="right" sx={{ minWidth: 220 }}>Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {sensors.map((s) => (
                      <TableRow key={s.sensorId} hover>
                        <TableCell>
                          <Chip label="Sensor" size="small" variant="outlined" sx={{ mr: 1, verticalAlign: 'middle' }} />
                          <Typography component="span" variant="body2" fontWeight={600}>{s.sensorName}</Typography>
                          <Typography variant="caption" display="block" color="text.secondary">
                            ID {s.sensorId}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2">{s.sensorType}</Typography>
                          {s.unit ? (
                            <Typography variant="caption" color="text.secondary" display="block">{s.unit}</Typography>
                          ) : null}
                        </TableCell>
                        <TableCell align="center">
                          <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', width: '100%' }}>
                            <Chip
                              size="small"
                              label={s.isActive ? 'Active' : 'Inactive'}
                              color={s.isActive ? 'success' : 'default'}
                              sx={statusChipSx}
                            />
                          </Box>
                        </TableCell>
                        <TableCell align="right">
                          <Stack direction="row" spacing={0.5} justifyContent="flex-end" alignItems="center" flexWrap="wrap" useFlexGap>
                            <IconButton size="small" aria-label="edit sensor" onClick={() => openSensorForm('edit', s)}>
                              <EditIcon fontSize="small" />
                            </IconButton>
                            <IconButton size="small" aria-label="delete sensor" onClick={() => deleteSensorRow(s)}>
                              <DeleteOutlineIcon fontSize="small" />
                            </IconButton>
                            <Button
                              variant="outlined"
                              size="small"
                              onClick={() => setSensorChartSensorId(s.sensorId)}
                            >
                              View
                            </Button>
                          </Stack>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </Paper>
        </Grid>

        <Grid item xs={12}>
          <Paper sx={panelSx}>
            <Box sx={sectionHeaderSx}>
              <Typography variant="h6" sx={{ lineHeight: 1.25 }}>Actuators (outputs)</Typography>
              <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                <Button component={RouterLink} to={`/actuators?deviceId=${device.deviceId}`} variant="outlined" size="small">
                  Manage actuators
                </Button>
                <Button startIcon={<AddIcon />} variant="contained" size="small" onClick={() => openActuatorDialog('add')}>
                  Add actuator
                </Button>
              </Stack>
            </Box>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Link a <strong>feedback sensor</strong> here (setting only). To see live readings and charts, use <strong>View</strong> on that sensor in Sensors above (same chart opens below).{' '}
              <strong>Control</strong> sends commands; detailed feedback still appears in Control while operating.
            </Typography>
            {actuators.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                No actuators yet. Add one to target <strong>SetPower</strong> / <strong>SetValue</strong> at a specific output.
              </Typography>
            ) : (
              <TableContainer>
                <Table size="small" sx={tableAlignSx}>
                  <TableHead>
                    <TableRow>
                      <TableCell>Actuator</TableCell>
                      <TableCell>Kind / channel</TableCell>
                      <TableCell sx={{ minWidth: 200, maxWidth: 280 }}>Feedback sensor</TableCell>
                      <TableCell align="center" sx={{ width: 120 }}>Status</TableCell>
                      <TableCell align="right" sx={{ minWidth: 200 }}>Actions</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {actuators.map((a) => (
                      <TableRow key={a.actuatorId} hover>
                        <TableCell>
                          <Chip label="Actuator" size="small" color="primary" variant="outlined" sx={{ mr: 1, verticalAlign: 'middle' }} />
                          <Typography component="span" variant="body2" fontWeight={600}>{a.name}</Typography>
                          <Typography variant="caption" display="block" color="text.secondary">
                            ID {a.actuatorId}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2">{a.kind}</Typography>
                          {a.channel ? (
                            <Typography variant="caption" color="text.secondary" display="block">{a.channel}</Typography>
                          ) : null}
                        </TableCell>
                        <TableCell>
                          <FormControl
                            size="small"
                            fullWidth
                            disabled={feedbackLinkSavingId === a.actuatorId || sensors.length === 0}
                            sx={{ maxWidth: 260 }}
                          >
                            <InputLabel id={`fb-act-${a.actuatorId}`}>Sensor</InputLabel>
                            <Select
                              labelId={`fb-act-${a.actuatorId}`}
                              label="Sensor"
                              value={a.feedbackSensorId ?? ''}
                              onChange={(e) => {
                                const v = e.target.value;
                                const nextId = v === '' ? undefined : Number(v);
                                void handleFeedbackSensorChange(a, nextId);
                              }}
                            >
                              <MenuItem value="">
                                <em>None</em>
                              </MenuItem>
                              {sensors.map((s) => (
                                <MenuItem key={s.sensorId} value={s.sensorId}>
                                  {s.sensorName} (ID {s.sensorId})
                                </MenuItem>
                              ))}
                            </Select>
                          </FormControl>
                          {sensors.length === 0 ? (
                            <Typography variant="caption" color="warning.main" display="block" sx={{ mt: 0.5 }}>
                              Add sensors above first.
                            </Typography>
                          ) : a.feedbackSensorId != null ? (
                            <Box sx={{ mt: 0.75 }}>
                              <Typography variant="caption" color="text.secondary" display="block" sx={{ lineHeight: 1.35 }}>
                                Monitor this input under <strong>Sensors</strong> → <strong>View</strong> on that row, or:
                              </Typography>
                              <Button
                                size="small"
                                variant="text"
                                sx={{ p: 0, mt: 0.25, minWidth: 0, fontSize: '0.8125rem', textTransform: 'none' }}
                                onClick={() => setSensorChartSensorId(a.feedbackSensorId!)}
                              >
                                Open live chart (same as sensor View)
                              </Button>
                            </Box>
                          ) : (
                            <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.5 }}>
                              Optional — then view status in Sensors above.
                            </Typography>
                          )}
                        </TableCell>
                        <TableCell align="center">
                          <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', width: '100%' }}>
                            <Chip
                              size="small"
                              label={a.isActive ? 'Active' : 'Inactive'}
                              color={a.isActive ? 'success' : 'default'}
                              sx={statusChipSx}
                            />
                          </Box>
                        </TableCell>
                        <TableCell align="right">
                          <Stack direction="row" spacing={0.5} justifyContent="flex-end" alignItems="center" flexWrap="wrap" useFlexGap>
                            <IconButton
                              size="small"
                              aria-label="edit actuator"
                              onClick={() => openActuatorDialog('edit', a)}
                            >
                              <EditIcon fontSize="small" />
                            </IconButton>
                            <IconButton size="small" aria-label="delete actuator" onClick={() => deleteActuator(a)}>
                              <DeleteOutlineIcon fontSize="small" />
                            </IconButton>
                            <Button
                              variant="contained"
                              size="small"
                              onClick={() => {
                                setActuatorControlTarget(a);
                                void refreshFeedbackForActuators(actuators);
                              }}
                            >
                              Control
                            </Button>
                          </Stack>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            )}
          </Paper>
        </Grid>

        {isControllableDevice && (
          <Grid item xs={12}>
            <Paper sx={panelSx}>
              <Typography variant="h6" sx={{ mb: 1 }}>
                Command status
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                Latest command for this device (including per-actuator commands). Refresh after MQTT acknowledgment if needed.
              </Typography>
              {commandError && <MuiAlert severity="error" sx={{ mb: 2 }}>{commandError}</MuiAlert>}
              {commandSuccess && <MuiAlert severity="success" sx={{ mb: 2 }}>{commandSuccess}</MuiAlert>}
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
                <Typography variant="body2" color="text.secondary">Latest command:</Typography>
                {latestCommand ? (
                  <>
                    <Chip size="small" label={`#${latestCommand.commandId}`} />
                    <Chip size="small" label={latestCommand.commandType} />
                    {latestCommand.actuatorId != null && (
                      <Chip size="small" variant="outlined" label={`Actuator ${latestCommand.actuatorId}`} />
                    )}
                    <Chip
                      size="small"
                      color={latestCommand.status === 'Failed' ? 'error' : latestCommand.status === 'Acked' ? 'success' : 'info'}
                      label={latestCommand.status}
                    />
                    <Typography variant="caption" color="text.secondary">
                      {new Date(latestCommand.createdAt).toLocaleString()}
                    </Typography>
                  </>
                ) : (
                  <Typography variant="body2">No commands yet</Typography>
                )}
                <Button size="small" onClick={() => refreshLatestCommand(device.deviceId)} disabled={commandLoading}>
                  Refresh status
                </Button>
              </Box>
            </Paper>
          </Grid>
        )}

        {useLegacyDeviceControl && (
          <Grid item xs={12}>
            <Paper sx={panelSx}>
              <Typography variant="h6" sx={{ mb: 1 }}>
                Device Control (legacy)
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                Submit control commands without per-actuator targeting. Add actuators above to hide this section.
              </Typography>

              <Stack direction={{ xs: 'column', md: 'row' }} spacing={3} alignItems={{ xs: 'stretch', md: 'center' }} sx={{ mb: 2 }}>
                {capabilityDraft.supportsPowerControl && (
                  <FormControlLabel
                    control={(
                      <Switch
                        checked={powerTarget}
                        disabled={commandLoading}
                        onChange={(event) => {
                          const next = event.target.checked;
                          setPowerTarget(next);
                          submitPowerCommand(next);
                        }}
                      />
                    )}
                    label={powerTarget ? 'Power target: ON' : 'Power target: OFF'}
                  />
                )}

                {capabilityDraft.supportsAnalogControl && (
                  <>
                    <Box sx={{ minWidth: 260, flexGrow: 1 }}>
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                        Analog target value: {analogTarget}{capabilityDraft.controlUnit ? ` ${capabilityDraft.controlUnit}` : ''}
                      </Typography>
                      <Slider
                        value={analogTarget}
                        min={capabilityDraft.analogMin}
                        max={capabilityDraft.analogMax}
                        step={capabilityDraft.analogStep}
                        disabled={commandLoading}
                        onChange={(_, value) => setAnalogTarget(value as number)}
                      />
                    </Box>

                    <TextField
                      label="Value"
                      type="number"
                      size="small"
                      value={analogTarget}
                      onChange={(event) => setAnalogTarget(Number(event.target.value))}
                      sx={{ width: 140 }}
                      inputProps={{ min: capabilityDraft.analogMin, max: capabilityDraft.analogMax, step: capabilityDraft.analogStep }}
                      disabled={commandLoading}
                    />

                    <Button variant="contained" onClick={submitAnalogCommand} disabled={commandLoading}>
                      Send SetValue
                    </Button>
                  </>
                )}
              </Stack>
            </Paper>
          </Grid>
        )}
      </Grid>

      <Dialog
        open={sensorChartSensorId != null}
        onClose={() => setSensorChartSensorId(null)}
        maxWidth="lg"
        fullWidth
      >
        <DialogTitle>
          Live readings —{' '}
          {sensorChartSensorId != null
            ? sensors.find((x) => x.sensorId === sensorChartSensorId)?.sensorName ?? `Sensor ${sensorChartSensorId}`
            : ''}
        </DialogTitle>
        <DialogContent dividers sx={{ pt: 2 }}>
          {sensorChartSensorId != null && (
            <DeviceTemperatureChart
              key={sensorChartSensorId}
              deviceId={device.deviceId}
              deviceName={device.deviceName}
              sensorId={sensorChartSensorId}
              height={420}
              showPaper={false}
              windowMode="time"
              timeWindowMinutes={5}
            />
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setSensorChartSensorId(null)}>Close</Button>
        </DialogActions>
      </Dialog>

      <Dialog
        open={actuatorControlTarget != null}
        onClose={() => setActuatorControlTarget(null)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>
          Control — {actuatorControlTarget?.name ?? ''}
        </DialogTitle>
        <DialogContent dividers>
          {actuatorControlTarget && (
            <>
              {!actuatorControlTarget.isActive && (
                <MuiAlert severity="warning" sx={{ mb: 2 }}>
                  This actuator is inactive. Turn it on from the Actuators tab or edit dialog before sending commands.
                </MuiAlert>
              )}
              {commandError && <MuiAlert severity="error" sx={{ mb: 2 }} onClose={() => setCommandError(null)}>{commandError}</MuiAlert>}
              {commandSuccess && <MuiAlert severity="success" sx={{ mb: 2 }} onClose={() => setCommandSuccess(null)}>{commandSuccess}</MuiAlert>}

              <Typography variant="body2" color="text.secondary" gutterBottom>
                {actuatorControlTarget.kind}
                {actuatorControlTarget.channel ? ` • ${actuatorControlTarget.channel}` : ''}
              </Typography>

              {actuatorControlTarget.feedbackSensorId != null && (
                <Box sx={{ mb: 2 }}>
                  <Typography variant="subtitle2" gutterBottom>
                    Feedback
                  </Typography>
                  <Typography variant="body2">
                    {sensorLabel(actuatorControlTarget.feedbackSensorId)} —{' '}
                    <strong>
                      {feedbackBySensorId[actuatorControlTarget.feedbackSensorId] === undefined
                        ? '—'
                        : actuatorControlTarget.kind.toLowerCase() === 'discrete'
                          ? (feedbackBySensorId[actuatorControlTarget.feedbackSensorId] ?? 0) >= 1
                            ? 'ON'
                            : 'OFF'
                          : String(feedbackBySensorId[actuatorControlTarget.feedbackSensorId])}
                      {actuatorControlTarget.kind.toLowerCase() === 'analog' && actuatorControlTarget.controlUnit
                        ? ` ${actuatorControlTarget.controlUnit}`
                        : ''}
                    </strong>
                  </Typography>
                  <Button size="small" sx={{ mt: 1 }} onClick={() => refreshFeedbackForActuators(actuators)} disabled={commandLoading}>
                    Refresh feedback
                  </Button>
                </Box>
              )}

              {actuatorControlTarget.kind.toLowerCase() === 'discrete' && (
                <FormControlLabel
                  sx={{ mt: 1, display: 'block' }}
                  control={(
                    <Switch
                      checked={powerByActuatorId[actuatorControlTarget.actuatorId] ?? false}
                      disabled={commandLoading || !actuatorControlTarget.isActive}
                      onChange={(_, v) => {
                        setPowerByActuatorId((p) => ({ ...p, [actuatorControlTarget.actuatorId]: v }));
                        void submitActuatorPower(actuatorControlTarget, v);
                      }}
                    />
                  )}
                  label={powerByActuatorId[actuatorControlTarget.actuatorId] ? 'Target ON' : 'Target OFF'}
                />
              )}

              {actuatorControlTarget.kind.toLowerCase() === 'analog' && (
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems={{ xs: 'stretch', sm: 'center' }} sx={{ mt: 2 }}>
                  <Box sx={{ flex: 1, minWidth: 200 }}>
                    <Typography variant="caption" color="text.secondary">
                      Value ({actuatorControlTarget.controlUnit || 'units'})
                    </Typography>
                    <Slider
                      value={analogByActuatorId[actuatorControlTarget.actuatorId] ?? (actuatorControlTarget.analogMin ?? 0)}
                      min={actuatorControlTarget.analogMin ?? 0}
                      max={actuatorControlTarget.analogMax ?? 100}
                      step={0.25}
                      disabled={commandLoading || !actuatorControlTarget.isActive}
                      onChange={(_, v) => setAnalogByActuatorId((p) => ({ ...p, [actuatorControlTarget.actuatorId]: v as number }))}
                    />
                  </Box>
                  <TextField
                    size="small"
                    type="number"
                    label="Value"
                    value={analogByActuatorId[actuatorControlTarget.actuatorId] ?? ''}
                    onChange={(e) => setAnalogByActuatorId((p) => ({
                      ...p,
                      [actuatorControlTarget.actuatorId]: Number(e.target.value),
                    }))}
                    InputProps={{ inputProps: { min: actuatorControlTarget.analogMin, max: actuatorControlTarget.analogMax, step: 0.25 } }}
                    sx={{ width: 120 }}
                    disabled={commandLoading || !actuatorControlTarget.isActive}
                  />
                  <Button
                    variant="contained"
                    size="small"
                    onClick={() => submitActuatorValue(actuatorControlTarget)}
                    disabled={commandLoading || !actuatorControlTarget.isActive}
                  >
                    Send SetValue
                  </Button>
                </Stack>
              )}
            </>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setActuatorControlTarget(null)}>Close</Button>
          <Button
            size="small"
            onClick={() => refreshLatestCommand(device.deviceId)}
            disabled={commandLoading}
          >
            Refresh command status
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={actuatorDialogOpen} onClose={() => setActuatorDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>{actuatorEditingId == null ? 'Add actuator' : 'Edit actuator'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <TextField
              label="Name"
              required
              fullWidth
              value={actuatorForm.name}
              onChange={(e) => setActuatorForm((f) => ({ ...f, name: e.target.value }))}
            />
            <TextField
              label="Description"
              fullWidth
              multiline
              minRows={2}
              value={actuatorForm.description ?? ''}
              onChange={(e) => setActuatorForm((f) => ({ ...f, description: e.target.value }))}
            />
            <FormControl fullWidth>
              <InputLabel id="act-kind-label">Kind</InputLabel>
              <Select
                labelId="act-kind-label"
                label="Kind"
                value={actuatorForm.kind}
                onChange={(e) => setActuatorForm((f) => ({ ...f, kind: String(e.target.value) }))}
              >
                <MenuItem value="Discrete">Discrete (SetPower / on-off)</MenuItem>
                <MenuItem value="Analog">Analog (SetValue)</MenuItem>
              </Select>
            </FormControl>
            <TextField
              label="Channel (optional, for firmware routing)"
              fullWidth
              value={actuatorForm.channel ?? ''}
              onChange={(e) => setActuatorForm((f) => ({ ...f, channel: e.target.value }))}
            />
            {actuatorForm.kind === 'Analog' && (
              <>
                <TextField
                  label="Analog min"
                  type="number"
                  fullWidth
                  value={actuatorForm.analogMin ?? ''}
                  onChange={(e) =>
                    setActuatorForm((f) => ({
                      ...f,
                      analogMin: e.target.value === '' ? undefined : Number(e.target.value),
                    }))
                  }
                />
                <TextField
                  label="Analog max"
                  type="number"
                  fullWidth
                  value={actuatorForm.analogMax ?? ''}
                  onChange={(e) =>
                    setActuatorForm((f) => ({
                      ...f,
                      analogMax: e.target.value === '' ? undefined : Number(e.target.value),
                    }))
                  }
                />
                <TextField
                  label="Control unit"
                  fullWidth
                  value={actuatorForm.controlUnit ?? ''}
                  onChange={(e) => setActuatorForm((f) => ({ ...f, controlUnit: e.target.value }))}
                />
              </>
            )}
            <FormControl fullWidth>
              <InputLabel id="act-fb-label">Feedback sensor</InputLabel>
              <Select
                labelId="act-fb-label"
                label="Feedback sensor"
                value={actuatorForm.feedbackSensorId ?? ''}
                onChange={(e) => {
                  const v = e.target.value;
                  setActuatorForm((f) => ({
                    ...f,
                    feedbackSensorId: v === '' ? undefined : Number(v),
                  }));
                }}
              >
                <MenuItem value="">None</MenuItem>
                {sensors.map((s) => (
                  <MenuItem key={s.sensorId} value={s.sensorId}>
                    {s.sensorName} (#{s.sensorId})
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setActuatorDialogOpen(false)}>Cancel</Button>
          <Button variant="contained" onClick={() => void saveActuatorDialog()} disabled={actuatorSaving}>
            {actuatorSaving ? 'Saving…' : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>

      <SensorForm
        key={editingSensor ? `edit-${editingSensor.sensorId}` : 'create'}
        open={sensorFormOpen}
        onClose={closeSensorForm}
        onSubmit={handleSubmitSensorForm}
        initialData={editingSensor ? sensorToFormValues(editingSensor) : undefined}
        title={editingSensor ? 'Edit sensor' : 'Add sensor'}
        isEdit={Boolean(editingSensor)}
      />
    </Box>
  );
};

export default DeviceDetailPage;
