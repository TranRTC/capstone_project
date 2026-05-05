import React, { useEffect, useState } from 'react';
import { Grid, Paper, Typography, Box, Card, CardContent, Chip, IconButton, Tooltip, Button, Skeleton, Alert as MuiAlert, useTheme } from '@mui/material';
import { CheckCircle, Error as ErrorIcon, Warning, Refresh, Devices, Sensors, NotificationsActive, Timeline } from '@mui/icons-material';
import * as signalR from '@microsoft/signalr';
import { Link } from 'react-router-dom';
import { apiService } from '../services/api';
import { signalRService } from '../services/signalRService';
import { DeviceList, Alert, SensorReading } from '../types';

type SignalRStatus = 'connected' | 'reconnecting' | 'disconnected';

const Dashboard: React.FC = () => {
  const theme = useTheme();
  const [devices, setDevices] = useState<DeviceList[]>([]);
  const [activeAlerts, setActiveAlerts] = useState<Alert[]>([]);
  const [latestReading, setLatestReading] = useState<SensorReading | null>(null);
  const [signalRStatus, setSignalRStatus] = useState<SignalRStatus>('disconnected');
  const [apiHealthy, setApiHealthy] = useState<boolean | null>(null);
  const [mqttStatus, setMqttStatus] = useState<string | null>(null);
  const [statusLoading, setStatusLoading] = useState(false);
  const [lastStatusCheck, setLastStatusCheck] = useState<Date | null>(null);
  const [deviceLastSeenMap, setDeviceLastSeenMap] = useState<Record<number, string>>({});
  const [dashboardLoading, setDashboardLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    loadData();
    connectSignalR();
    checkSystemStatus();

    const statusInterval = setInterval(checkSystemStatus, 30000);
    const signalRStateInterval = setInterval(() => {
      const state = signalRService.getConnectionState();
      if (state === signalR.HubConnectionState.Connected) {
        setSignalRStatus('connected');
      } else if (state === signalR.HubConnectionState.Reconnecting) {
        setSignalRStatus('reconnecting');
      } else {
        setSignalRStatus('disconnected');
      }
    }, 5000);

    return () => {
      clearInterval(statusInterval);
      clearInterval(signalRStateInterval);
      signalRService.stop();
    };
  }, []);

  const loadData = async () => {
    setDashboardLoading(true);
    setLoadError(null);
    try {
      const [devicesData, alertsData] = await Promise.all([
        apiService.getDevices(),
        apiService.getActiveAlerts(),
      ]);
      setDevices(devicesData);
      setActiveAlerts(alertsData);
      setDeviceLastSeenMap((prev) => {
        const initialMap = { ...prev };
        devicesData.forEach((device) => {
          if (device.lastSeenAt && !initialMap[device.deviceId]) {
            initialMap[device.deviceId] = device.lastSeenAt;
          }
        });
        return initialMap;
      });
    } catch (error: any) {
      console.error('Error loading dashboard data:', error.message || error);
      // Set empty arrays on error to prevent UI issues
      setDevices([]);
      setActiveAlerts([]);
      setLoadError(error?.message || 'Failed to load dashboard data');
    } finally {
      setDashboardLoading(false);
    }
  };

  const connectSignalR = async () => {
    try {
      await signalRService.start();
      
      // Wait a moment to ensure connection is stable
      await new Promise(resolve => setTimeout(resolve, 100));
      
      const state = signalRService.getConnectionState();
      if (state === signalR.HubConnectionState.Connected) {
        setSignalRStatus('connected');

        await signalRService.subscribeToAllDevices();
        await signalRService.subscribeToAlerts();

        signalRService.onSensorReading((reading) => {
          setLatestReading(reading);
          setDeviceLastSeenMap((prev) => ({
            ...prev,
            [reading.deviceId]: reading.timestamp,
          }));
        });

        signalRService.onNewAlert((alert) => {
          setActiveAlerts((prev) => [alert, ...prev]);
        });

        signalRService.onAlertResolved((alert) => {
          setActiveAlerts((prev) => prev.filter((a) => a.alertId !== alert.alertId));
        });

        signalRService.onReconnecting(() => {
          setSignalRStatus('reconnecting');
        });

        signalRService.onReconnected(async () => {
          setSignalRStatus('connected');
          await signalRService.subscribeToAllDevices();
          await signalRService.subscribeToAlerts();
        });

        signalRService.onClose(() => {
          setSignalRStatus('disconnected');
        });
      } else {
        setSignalRStatus('disconnected');
      }
    } catch (error: any) {
      // Ignore AbortError and negotiation errors - automatic reconnect will handle them
      // These are transient issues that resolve automatically
      if (error?.name !== 'AbortError' && 
          !(error?.message && error.message.includes('negotiation'))) {
        console.error('SignalR connection error:', error);
      }
      // Don't set connected to false immediately - let automatic reconnect try
      // The connection state will update when it successfully connects
      setSignalRStatus('disconnected');
    }
  };

  const checkSystemStatus = async () => {
    setStatusLoading(true);
    try {
      const [healthData, mqttData] = await Promise.all([
        apiService.checkHealth(),
        apiService.checkMqttBrokerStatus(),
      ]);

      setApiHealthy(healthData.status === 'healthy');
      setMqttStatus(mqttData.status);
    } catch (error) {
      setApiHealthy(false);
      setMqttStatus('error');
    } finally {
      setLastStatusCheck(new Date());
      setStatusLoading(false);
    }
  };

  const getMqttStatusChip = () => {
    if (!mqttStatus) {
      return {
        label: 'MQTT Unknown',
        color: 'default' as const,
        icon: <ErrorIcon fontSize="small" />,
      };
    }
    if (mqttStatus === 'ready') {
      return {
        label: 'MQTT Ready',
        color: 'success' as const,
        icon: <CheckCircle fontSize="small" />,
      };
    }
    if (mqttStatus === 'port_open_but_mqtt_failed') {
      return {
        label: 'MQTT Warning',
        color: 'warning' as const,
        icon: <Warning fontSize="small" />,
      };
    }
    return {
      label: 'MQTT Unavailable',
      color: 'error' as const,
      icon: <ErrorIcon fontSize="small" />,
    };
  };

  const getDataFlowChip = () => {
    if (!latestReading) {
      return {
        label: 'Data: No readings',
        color: 'default' as const,
        icon: <ErrorIcon fontSize="small" />,
      };
    }

    const secondsAgo = Math.floor(
      (Date.now() - new Date(latestReading.timestamp).getTime()) / 1000
    );

    if (secondsAgo <= 120) {
      return {
        label: `Data Fresh (${secondsAgo}s)`,
        color: 'success' as const,
        icon: <CheckCircle fontSize="small" />,
      };
    }
    if (secondsAgo <= 300) {
      return {
        label: `Data Delayed (${secondsAgo}s)`,
        color: 'warning' as const,
        icon: <Warning fontSize="small" />,
      };
    }
    return {
      label: `Data Stale (${secondsAgo}s)`,
      color: 'error' as const,
      icon: <ErrorIcon fontSize="small" />,
    };
  };

  const getAgeSeconds = (timestamp?: string) => {
    if (!timestamp) return null;
    return Math.floor((Date.now() - new Date(timestamp).getTime()) / 1000);
  };

  const getFreshnessStatus = (ageSeconds: number | null) => {
    if (ageSeconds === null) {
      return {
        label: 'No data',
        color: 'default' as const,
      };
    }

    if (ageSeconds <= 120) {
      return {
        label: 'Fresh',
        color: 'success' as const,
      };
    }

    if (ageSeconds <= 300) {
      return {
        label: 'Delayed',
        color: 'warning' as const,
      };
    }

    return {
      label: 'Stale',
      color: 'error' as const,
    };
  };

  const formatAge = (ageSeconds: number | null) => {
    if (ageSeconds === null) return 'Never';
    if (ageSeconds < 60) return `${ageSeconds}s ago`;
    if (ageSeconds < 3600) return `${Math.floor(ageSeconds / 60)}m ago`;
    return `${Math.floor(ageSeconds / 3600)}h ago`;
  };

  const activeDevices = devices.filter((d) => d.isActive).length;
  const totalDevices = devices.length;
  const mqttChip = getMqttStatusChip();
  const dataFlowChip = getDataFlowChip();
  const signalRChip = {
    label:
      signalRStatus === 'connected'
        ? 'SignalR Connected'
        : signalRStatus === 'reconnecting'
          ? 'SignalR Reconnecting'
          : 'SignalR Disconnected',
    color:
      signalRStatus === 'connected'
        ? ('success' as const)
        : signalRStatus === 'reconnecting'
          ? ('warning' as const)
          : ('error' as const),
    icon:
      signalRStatus === 'connected'
        ? <CheckCircle fontSize="small" />
        : signalRStatus === 'reconnecting'
          ? <Warning fontSize="small" />
          : <ErrorIcon fontSize="small" />,
  };
  const deviceLastSeenRows = devices
    .map((device) => {
      const timestamp = deviceLastSeenMap[device.deviceId] ?? device.lastSeenAt;
      const ageSeconds = getAgeSeconds(timestamp);
      return {
        deviceId: device.deviceId,
        deviceName: device.deviceName,
        timestamp,
        ageSeconds,
      };
    })
    .sort((a, b) => {
      const ageA = a.ageSeconds ?? Number.MAX_SAFE_INTEGER;
      const ageB = b.ageSeconds ?? Number.MAX_SAFE_INTEGER;
      return ageB - ageA;
    })
    .slice(0, 5);
  const severityRank: Record<string, number> = {
    critical: 4,
    high: 3,
    medium: 2,
    low: 1,
  };
  const sortedRecentAlerts = [...activeAlerts]
    .sort((a, b) => {
      const severityDiff =
        (severityRank[b.severity?.toLowerCase()] ?? 0) -
        (severityRank[a.severity?.toLowerCase()] ?? 0);
      if (severityDiff !== 0) return severityDiff;
      return new Date(b.triggeredAt).getTime() - new Date(a.triggeredAt).getTime();
    })
    .slice(0, 5);
  const metricCardSx = { height: '100%' };
  const panelSx = {
    p: 2.5,
    height: '100%',
    border: 1,
    borderColor: 'divider',
    boxShadow: 'none',
    bgcolor: 'background.paper',
  };
  const rowSx = {
    borderRadius: theme.shape.borderRadius,
  };

  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom sx={{ mb: 2 }}>
        Dashboard
      </Typography>

      <Box sx={{ mb: 3 }}>
        <Typography variant="h6" component="h2" sx={{ mb: 2, fontWeight: 600 }}>
          System Status
        </Typography>
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: {
              xs: '1fr',
              sm: 'repeat(2, minmax(0, 1fr))',
              md: 'repeat(5, minmax(0, 1fr))',
            },
            gap: 3,
            width: '100%',
            alignItems: 'stretch',
          }}
        >
          <Card sx={metricCardSx}>
            <CardContent>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                  API
                </Typography>
                {apiHealthy === null ? (
                  <Warning fontSize="small" color="warning" />
                ) : apiHealthy ? (
                  <CheckCircle fontSize="small" color="success" />
                ) : (
                  <ErrorIcon fontSize="small" color="error" />
                )}
              </Box>
              <Chip
                icon={
                  apiHealthy === null
                    ? <Warning fontSize="small" />
                    : apiHealthy
                      ? <CheckCircle fontSize="small" />
                      : <ErrorIcon fontSize="small" />
                }
                label={
                  apiHealthy === null
                    ? 'API Checking'
                    : apiHealthy
                      ? 'API Healthy'
                      : 'API Down'
                }
                color={apiHealthy === null ? 'warning' : apiHealthy ? 'success' : 'error'}
                sx={{ maxWidth: '100%' }}
              />
            </CardContent>
          </Card>

          <Card sx={metricCardSx}>
            <CardContent>
              <Typography variant="subtitle2" color="text.secondary" gutterBottom sx={{ mb: 1 }}>
                MQTT
              </Typography>
              <Chip
                icon={mqttChip.icon}
                label={mqttChip.label}
                color={mqttChip.color}
                sx={{ maxWidth: '100%' }}
              />
            </CardContent>
          </Card>

          <Card sx={metricCardSx}>
            <CardContent>
              <Typography variant="subtitle2" color="text.secondary" gutterBottom sx={{ mb: 1 }}>
                SignalR
              </Typography>
              <Chip
                icon={signalRChip.icon}
                label={signalRChip.label}
                color={signalRChip.color}
                sx={{ maxWidth: '100%' }}
              />
            </CardContent>
          </Card>

          <Card sx={metricCardSx}>
            <CardContent>
              <Typography variant="subtitle2" color="text.secondary" gutterBottom sx={{ mb: 1 }}>
                Data
              </Typography>
              <Chip
                icon={dataFlowChip.icon}
                label={dataFlowChip.label}
                color={dataFlowChip.color}
                sx={{ maxWidth: '100%' }}
              />
            </CardContent>
          </Card>

          <Card sx={metricCardSx}>
            <CardContent>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                  Last check
                </Typography>
                <Tooltip title="Refresh system checks">
                  <span>
                    <IconButton size="small" onClick={checkSystemStatus} disabled={statusLoading} aria-label="Refresh status">
                      <Refresh fontSize="small" />
                    </IconButton>
                  </span>
                </Tooltip>
              </Box>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                {lastStatusCheck
                  ? lastStatusCheck.toLocaleTimeString()
                  : 'Checking...'}
              </Typography>
            </CardContent>
          </Card>
        </Box>
      </Box>

      {loadError && (
        <MuiAlert
          severity="error"
          sx={{ mb: 3 }}
          action={(
            <Button color="inherit" size="small" onClick={loadData}>
              Retry
            </Button>
          )}
        >
          {loadError}
        </MuiAlert>
      )}

      <Grid container spacing={3}>
        <Grid item xs={12} sm={6} md={3}>
          <Card sx={metricCardSx}>
            <CardContent>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                  Total Devices
                </Typography>
                <Devices fontSize="small" color="action" />
              </Box>
              {dashboardLoading ? <Skeleton variant="text" width={80} height={40} /> : <Typography variant="h4">{totalDevices}</Typography>}
              <Typography variant="caption" color="text.secondary">
                Registered inventory
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card sx={metricCardSx}>
            <CardContent>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                  Active Devices
                </Typography>
                <Sensors fontSize="small" color="action" />
              </Box>
              {dashboardLoading ? <Skeleton variant="text" width={80} height={40} /> : <Typography variant="h4">{activeDevices}</Typography>}
              <Typography variant="caption" color="text.secondary">
                Devices marked active
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card sx={metricCardSx}>
            <CardContent>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                  Active Alerts
                </Typography>
                <NotificationsActive fontSize="small" color="action" />
              </Box>
              {dashboardLoading ? (
                <Skeleton variant="text" width={80} height={40} />
              ) : (
                <Typography variant="h4" color={activeAlerts.length > 0 ? 'error' : 'text.primary'}>
                  {activeAlerts.length}
                </Typography>
              )}
              <Typography variant="caption" color="text.secondary">
                Open and unresolved
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card sx={metricCardSx}>
            <CardContent>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                  Latest Reading
                </Typography>
                <Timeline fontSize="small" color="action" />
              </Box>
              {dashboardLoading ? (
                <>
                  <Skeleton variant="text" width="90%" height={28} sx={{ mb: 1 }} />
                  <Skeleton variant="text" width="70%" height={20} sx={{ mb: 0.5 }} />
                  <Skeleton variant="text" width="85%" height={18} />
                </>
              ) : latestReading ? (
                <Box>
                  <Typography variant="body1" sx={{ fontWeight: 600 }}>
                    Value: {latestReading.value}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Device {latestReading.deviceId} • Sensor {latestReading.sensorId}
                  </Typography>
                  <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.5 }}>
                    {new Date(latestReading.timestamp).toLocaleString()}
                  </Typography>
                </Box>
              ) : (
                <Box>
                  <Typography variant="body1" sx={{ fontWeight: 600 }}>
                    Value: N/A
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    —
                  </Typography>
                  <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.5 }}>
                    No readings yet
                  </Typography>
                </Box>
              )}
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={6}>
          <Paper sx={panelSx}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
              <Typography variant="h6">
                Device Last Seen
              </Typography>
              <Button
                size="small"
                component={Link}
                to="/devices"
              >
                View all devices
              </Button>
            </Box>
            {deviceLastSeenRows.length === 0 ? (
              <Typography color="text.secondary">No devices available</Typography>
            ) : (
              <Box>
                {deviceLastSeenRows.map((device) => {
                  const freshness = getFreshnessStatus(device.ageSeconds);
                  return (
                    <Box
                      key={device.deviceId}
                      sx={{
                        ...rowSx,
                        mb: 1,
                        p: 1,
                        bgcolor: 'background.default',
                        textDecoration: 'none',
                        color: 'inherit',
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'center',
                        gap: 1,
                        '&:hover': {
                          bgcolor: 'action.hover',
                        },
                      }}
                      component={Link}
                      to={`/devices/${device.deviceId}`}
                    >
                      <Box>
                        <Typography variant="body2">
                          {device.deviceName}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {device.timestamp ? new Date(device.timestamp).toLocaleString() : 'No readings received'}
                        </Typography>
                      </Box>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Typography variant="caption" color="text.secondary">
                          {formatAge(device.ageSeconds)}
                        </Typography>
                        <Chip
                          size="small"
                          label={freshness.label}
                          color={freshness.color}
                        />
                      </Box>
                    </Box>
                  );
                })}
              </Box>
            )}
          </Paper>
        </Grid>

        <Grid item xs={12} md={6}>
          <Paper sx={panelSx}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
              <Typography variant="h6">
                Recent Alerts
              </Typography>
              <Button
                size="small"
                component={Link}
                to="/alerts"
              >
                View all alerts
              </Button>
            </Box>
            {sortedRecentAlerts.length === 0 ? (
              <Typography color="text.secondary">No active alerts</Typography>
            ) : (
              <Box>
                {sortedRecentAlerts.map((alert) => (
                  <Box
                    key={alert.alertId}
                    sx={{
                      ...rowSx,
                      mb: 1,
                      p: 1,
                      bgcolor: 'background.default',
                      textDecoration: 'none',
                      color: 'inherit',
                      '&:hover': {
                        bgcolor: 'action.hover',
                      },
                    }}
                    component={Link}
                    to="/alerts"
                  >
                    <Typography variant="body2">
                      <strong>{alert.severity}:</strong> {alert.message}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      Device {alert.deviceId} • {new Date(alert.triggeredAt).toLocaleString()}
                    </Typography>
                  </Box>
                ))}
              </Box>
            )}
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
};

export default Dashboard;

