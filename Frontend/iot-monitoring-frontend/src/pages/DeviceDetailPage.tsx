import React, { useEffect, useState } from 'react';
import { useParams, useNavigate, Link as RouterLink } from 'react-router-dom';
import {
  Box,
  Typography,
  Paper,
  Grid,
  Card,
  CardContent,
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
  Divider,
} from '@mui/material';
import { apiService } from '../services/api';
import { Device, DeviceCommand, DeviceConfiguration } from '../types';

const panelSx = {
  p: 2.5,
  border: 1,
  borderColor: 'divider',
  boxShadow: 'none',
  bgcolor: 'background.paper',
};

const DeviceDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [device, setDevice] = useState<Device | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [latestCommand, setLatestCommand] = useState<DeviceCommand | null>(null);
  const [deviceConfigurations, setDeviceConfigurations] = useState<DeviceConfiguration[]>([]);
  const [commandLoading, setCommandLoading] = useState(false);
  const [commandError, setCommandError] = useState<string | null>(null);
  const [commandSuccess, setCommandSuccess] = useState<string | null>(null);
  const [powerTarget, setPowerTarget] = useState(false);
  const [analogTarget, setAnalogTarget] = useState(50);
  const [configSaving, setConfigSaving] = useState(false);
  const [configMessage, setConfigMessage] = useState<string | null>(null);
  const [capabilityDraft, setCapabilityDraft] = useState({
    supportsTelemetry: true,
    supportsPowerControl: false,
    supportsAnalogControl: false,
    analogMin: 0,
    analogMax: 100,
    analogStep: 1,
    controlUnit: '',
  });

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

  const loadDevice = async (deviceId: number) => {
    try {
      setLoading(true);
      setError(null);
      const deviceData = await apiService.getDevice(deviceId);
      const configs = await apiService.getDeviceConfigurations(deviceId);
      const capabilities = buildCapabilityModel(configs, deviceData.deviceType);
      setDevice(deviceData);
      setDeviceConfigurations(configs);
      setAnalogTarget(capabilities.analogMin);
      setCapabilityDraft(capabilities);
      if (capabilities.supportsPowerControl || capabilities.supportsAnalogControl) {
        await refreshLatestCommand(deviceData.deviceId);
      } else {
        setLatestCommand(null);
      }
    } catch (err: any) {
      console.error('Error loading device:', err);
      setDevice(null);
      setDeviceConfigurations([]);
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

  const parsedId = id ? Number.parseInt(id, 10) : NaN;
  const canRetry = Number.isFinite(parsedId);
  const capabilities = buildCapabilityModel(deviceConfigurations, device?.deviceType);
  const isControllableDevice = capabilityDraft.supportsPowerControl || capabilityDraft.supportsAnalogControl;

  const saveCapabilities = async () => {
    if (!device) return;
    try {
      setConfigSaving(true);
      setConfigMessage(null);
      await apiService.updateDeviceConfigurations(device.deviceId, [
        { configurationKey: 'supportsTelemetry', configurationValue: String(capabilityDraft.supportsTelemetry), valueType: 'bool' },
        { configurationKey: 'supportsPowerControl', configurationValue: String(capabilityDraft.supportsPowerControl), valueType: 'bool' },
        { configurationKey: 'supportsAnalogControl', configurationValue: String(capabilityDraft.supportsAnalogControl), valueType: 'bool' },
        { configurationKey: 'analogMin', configurationValue: String(capabilityDraft.analogMin), valueType: 'number' },
        { configurationKey: 'analogMax', configurationValue: String(capabilityDraft.analogMax), valueType: 'number' },
        { configurationKey: 'analogStep', configurationValue: String(capabilityDraft.analogStep), valueType: 'number' },
        { configurationKey: 'controlUnit', configurationValue: capabilityDraft.controlUnit, valueType: 'string' },
      ]);
      const latestConfigs = await apiService.getDeviceConfigurations(device.deviceId);
      setDeviceConfigurations(latestConfigs);
      const updated = buildCapabilityModel(latestConfigs, device.deviceType);
      setCapabilityDraft(updated);
      setAnalogTarget(updated.analogMin);
      setConfigMessage('Capabilities saved.');
    } catch (err: any) {
      setConfigMessage(err.message || 'Failed to save capabilities.');
    } finally {
      setConfigSaving(false);
    }
  };

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

      <Typography variant="body2" color="text.secondary" sx={{ mb: 2, maxWidth: 640 }}>
        Device profile below. Live readings and charts are tied to sensors —{' '}
        <Link component={RouterLink} to={`/sensors?deviceId=${device.deviceId}`} underline="hover">
          open Sensors for this device
        </Link>
        .
      </Typography>

      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12}>
          <Paper sx={panelSx}>
            <Typography variant="h6" sx={{ mb: 1 }}>
              Capability Profile
            </Typography>
            <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', mb: 1.5 }}>
              <Chip
                size="small"
                color={capabilityDraft.supportsTelemetry ? 'success' : 'default'}
                label={`Telemetry: ${capabilityDraft.supportsTelemetry ? 'On' : 'Off'}`}
              />
              <Chip
                size="small"
                color={capabilityDraft.supportsPowerControl ? 'success' : 'default'}
                label={`Power Control: ${capabilityDraft.supportsPowerControl ? 'On' : 'Off'}`}
              />
              <Chip
                size="small"
                color={capabilityDraft.supportsAnalogControl ? 'success' : 'default'}
                label={`Analog Control: ${capabilityDraft.supportsAnalogControl ? 'On' : 'Off'}`}
              />
              {capabilityDraft.supportsAnalogControl && (
                <Chip
                  size="small"
                  variant="outlined"
                  label={`Range: ${capabilityDraft.analogMin} - ${capabilityDraft.analogMax} (step ${capabilityDraft.analogStep})${capabilityDraft.controlUnit ? ` ${capabilityDraft.controlUnit}` : ''}`}
                />
              )}
            </Box>
            <Divider sx={{ my: 1.5 }} />
            <Typography variant="subtitle2" sx={{ mb: 1 }}>Edit Capabilities</Typography>
            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ mb: 1.5 }}>
              <FormControlLabel
                control={<Switch checked={capabilityDraft.supportsTelemetry} onChange={(_, v) => setCapabilityDraft((p) => ({ ...p, supportsTelemetry: v }))} />}
                label="Telemetry"
              />
              <FormControlLabel
                control={<Switch checked={capabilityDraft.supportsPowerControl} onChange={(_, v) => setCapabilityDraft((p) => ({ ...p, supportsPowerControl: v }))} />}
                label="Power Control"
              />
              <FormControlLabel
                control={<Switch checked={capabilityDraft.supportsAnalogControl} onChange={(_, v) => setCapabilityDraft((p) => ({ ...p, supportsAnalogControl: v }))} />}
                label="Analog Control"
              />
            </Stack>
            {capabilityDraft.supportsAnalogControl && (
              <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5} sx={{ mb: 1.5 }}>
                <TextField label="Analog Min" type="number" size="small" value={capabilityDraft.analogMin} onChange={(e) => setCapabilityDraft((p) => ({ ...p, analogMin: Number(e.target.value) }))} />
                <TextField label="Analog Max" type="number" size="small" value={capabilityDraft.analogMax} onChange={(e) => setCapabilityDraft((p) => ({ ...p, analogMax: Number(e.target.value) }))} />
                <TextField label="Analog Step" type="number" size="small" value={capabilityDraft.analogStep} onChange={(e) => setCapabilityDraft((p) => ({ ...p, analogStep: Number(e.target.value) }))} />
                <TextField label="Control Unit" size="small" value={capabilityDraft.controlUnit} onChange={(e) => setCapabilityDraft((p) => ({ ...p, controlUnit: e.target.value }))} />
              </Stack>
            )}
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <Button variant="contained" size="small" onClick={saveCapabilities} disabled={configSaving}>
                {configSaving ? 'Saving...' : 'Save Capabilities'}
              </Button>
              {configMessage && (
                <Typography variant="caption" color={configMessage.includes('Failed') ? 'error.main' : 'success.main'}>
                  {configMessage}
                </Typography>
              )}
            </Box>
            <Typography variant="caption" color="text.secondary">
              Loaded configuration entries: {deviceConfigurations.length}
            </Typography>
          </Paper>
        </Grid>

        {isControllableDevice && (
          <Grid item xs={12}>
            <Paper sx={panelSx}>
              <Typography variant="h6" sx={{ mb: 1 }}>
                Device Control
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                Submit control commands for this device. Actual device state should be verified by telemetry or command acknowledgements.
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

              {commandError && <MuiAlert severity="error" sx={{ mb: 2 }}>{commandError}</MuiAlert>}
              {commandSuccess && <MuiAlert severity="success" sx={{ mb: 2 }}>{commandSuccess}</MuiAlert>}

              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
                <Typography variant="body2" color="text.secondary">Latest command:</Typography>
                {latestCommand ? (
                  <>
                    <Chip size="small" label={`#${latestCommand.commandId}`} />
                    <Chip size="small" label={latestCommand.commandType} />
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

        <Grid item xs={12}>
          <Paper sx={{ ...panelSx }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2, flexWrap: 'wrap', gap: 1 }}>
              <Typography variant="h4" component="h1">
                {device.deviceName}
              </Typography>
              <Chip
                label={device.isActive ? 'Active' : 'Inactive'}
                color={device.isActive ? 'success' : 'default'}
                size="medium"
              />
            </Box>

            <Grid container spacing={3}>
              <Grid item xs={12} md={6}>
                <Card variant="outlined" sx={{ boxShadow: 'none' }}>
                  <CardContent>
                    <Typography color="text.secondary" gutterBottom variant="body2">
                      Device ID
                    </Typography>
                    <Typography variant="h6">{device.deviceId}</Typography>
                  </CardContent>
                </Card>
              </Grid>

              <Grid item xs={12} md={6}>
                <Card variant="outlined" sx={{ boxShadow: 'none' }}>
                  <CardContent>
                    <Typography color="text.secondary" gutterBottom variant="body2">
                      Device Type
                    </Typography>
                    <Typography variant="h6">{device.deviceType}</Typography>
                  </CardContent>
                </Card>
              </Grid>

              {device.location && (
                <Grid item xs={12} md={6}>
                  <Card variant="outlined" sx={{ boxShadow: 'none' }}>
                    <CardContent>
                      <Typography color="text.secondary" gutterBottom variant="body2">
                        Location
                      </Typography>
                      <Typography variant="h6">{device.location}</Typography>
                    </CardContent>
                  </Card>
                </Grid>
              )}

              {device.facilityType && (
                <Grid item xs={12} md={6}>
                  <Card variant="outlined" sx={{ boxShadow: 'none' }}>
                    <CardContent>
                      <Typography color="text.secondary" gutterBottom variant="body2">
                        Facility Type
                      </Typography>
                      <Typography variant="h6">{device.facilityType}</Typography>
                    </CardContent>
                  </Card>
                </Grid>
              )}

              {device.edgeDeviceType && (
                <Grid item xs={12} md={6}>
                  <Card variant="outlined" sx={{ boxShadow: 'none' }}>
                    <CardContent>
                      <Typography color="text.secondary" gutterBottom variant="body2">
                        Edge Device Type
                      </Typography>
                      <Typography variant="h6">{device.edgeDeviceType}</Typography>
                    </CardContent>
                  </Card>
                </Grid>
              )}

              {device.edgeDeviceId && (
                <Grid item xs={12} md={6}>
                  <Card variant="outlined" sx={{ boxShadow: 'none' }}>
                    <CardContent>
                      <Typography color="text.secondary" gutterBottom variant="body2">
                        Edge Device ID
                      </Typography>
                      <Typography variant="h6">{device.edgeDeviceId}</Typography>
                    </CardContent>
                  </Card>
                </Grid>
              )}

              <Grid item xs={12} md={6}>
                <Card variant="outlined" sx={{ boxShadow: 'none' }}>
                  <CardContent>
                    <Typography color="text.secondary" gutterBottom variant="body2">
                      Last Seen
                    </Typography>
                    <Typography variant="h6">
                      {device.lastSeenAt
                        ? new Date(device.lastSeenAt).toLocaleString()
                        : 'Never'}
                    </Typography>
                  </CardContent>
                </Card>
              </Grid>

              <Grid item xs={12} md={6}>
                <Card variant="outlined" sx={{ boxShadow: 'none' }}>
                  <CardContent>
                    <Typography color="text.secondary" gutterBottom variant="body2">
                      Created At
                    </Typography>
                    <Typography variant="h6">
                      {new Date(device.createdAt).toLocaleString()}
                    </Typography>
                  </CardContent>
                </Card>
              </Grid>

            </Grid>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
};

export default DeviceDetailPage;
