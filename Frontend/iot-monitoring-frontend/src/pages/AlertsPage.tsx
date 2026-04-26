import React, { useEffect, useMemo, useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import {
  Box,
  Typography,
  Paper,
  Chip,
  Button,
  Stack,
  Skeleton,
  Alert as MuiAlert,
  Snackbar,
  Link,
} from '@mui/material';
import type { ChipProps } from '@mui/material/Chip';
import { apiService } from '../services/api';
import { signalRService } from '../services/signalRService';
import { Alert as AlertDto } from '../types';

const panelSx = {
  p: 2.5,
  border: 1,
  borderColor: 'divider',
  boxShadow: 'none',
  bgcolor: 'background.paper',
};

const SEVERITY_RANK: Record<string, number> = {
  critical: 4,
  high: 3,
  medium: 2,
  low: 1,
};

function sortAlerts(list: AlertDto[]): AlertDto[] {
  return [...list].sort((a, b) => {
    const severityDiff =
      (SEVERITY_RANK[b.severity?.toLowerCase()] ?? 0) -
      (SEVERITY_RANK[a.severity?.toLowerCase()] ?? 0);
    if (severityDiff !== 0) return severityDiff;
    return new Date(b.triggeredAt).getTime() - new Date(a.triggeredAt).getTime();
  });
}

function getSeverityColor(severity: string): ChipProps['color'] {
  switch (severity.toLowerCase()) {
    case 'critical':
      return 'error';
    case 'high':
      return 'warning';
    case 'medium':
      return 'info';
    default:
      return 'default';
  }
}

const AlertsPage: React.FC = () => {
  const [alerts, setAlerts] = useState<AlertDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [liveConnected, setLiveConnected] = useState<boolean | null>(null);
  const [pendingIds, setPendingIds] = useState<Set<number>>(() => new Set());
  const [snackbar, setSnackbar] = useState<{ open: boolean; message: string }>({
    open: false,
    message: '',
  });

  const sortedAlerts = useMemo(() => sortAlerts(alerts), [alerts]);

  useEffect(() => {
    loadAlerts();
    connectSignalR();

    const stateInterval = setInterval(() => {
      const state = signalRService.getConnectionState();
      if (state === signalR.HubConnectionState.Connected) {
        setLiveConnected(true);
      } else if (
        state === signalR.HubConnectionState.Reconnecting ||
        state === signalR.HubConnectionState.Connecting
      ) {
        setLiveConnected(false);
      } else {
        setLiveConnected(false);
      }
    }, 5000);

    return () => {
      clearInterval(stateInterval);
      signalRService.off('NewAlert');
      signalRService.off('AlertResolved');
      signalRService.off('AlertAcknowledged');
    };
  }, []);

  const loadAlerts = async () => {
    try {
      setLoading(true);
      setLoadError(null);
      const data = await apiService.getActiveAlerts();
      setAlerts(data);
    } catch (err: any) {
      console.error('Error loading alerts:', err);
      setLoadError(err?.message || 'Failed to load alerts');
      setAlerts([]);
    } finally {
      setLoading(false);
    }
  };

  const connectSignalR = async () => {
    try {
      await signalRService.start();
      await signalRService.subscribeToAlerts();

      signalRService.onNewAlert((alert) => {
        setAlerts((prev) => {
          if (prev.some((a) => a.alertId === alert.alertId)) return prev;
          return [alert, ...prev];
        });
      });

      signalRService.onAlertAcknowledged((updated) => {
        setAlerts((prev) =>
          prev.map((a) => (a.alertId === updated.alertId ? { ...a, ...updated } : a))
        );
      });

      signalRService.onAlertResolved((resolved) => {
        setAlerts((prev) => prev.filter((a) => a.alertId !== resolved.alertId));
      });

      const state = signalRService.getConnectionState();
      setLiveConnected(state === signalR.HubConnectionState.Connected);
    } catch (error) {
      console.error('SignalR connection error:', error);
      setLiveConnected(false);
    }
  };

  const setPending = (alertId: number, pending: boolean) => {
    setPendingIds((prev) => {
      const next = new Set(prev);
      if (pending) next.add(alertId);
      else next.delete(alertId);
      return next;
    });
  };

  const showActionError = (message: string) => {
    setSnackbar({ open: true, message });
  };

  const handleAcknowledge = async (alertId: number) => {
    setPending(alertId, true);
    try {
      await apiService.acknowledgeAlert(alertId);
      await loadAlerts();
    } catch (err: any) {
      console.error('Error acknowledging alert:', err);
      showActionError(err?.message || 'Failed to acknowledge alert');
    } finally {
      setPending(alertId, false);
    }
  };

  const handleResolve = async (alertId: number) => {
    setPending(alertId, true);
    try {
      await apiService.resolveAlert(alertId);
      await loadAlerts();
    } catch (err: any) {
      console.error('Error resolving alert:', err);
      showActionError(err?.message || 'Failed to resolve alert');
    } finally {
      setPending(alertId, false);
    }
  };

  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom sx={{ mb: 2 }}>
        Alerts
      </Typography>

      <Box sx={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 1, mb: 2 }}>
        <Typography variant="subtitle2" color="text.secondary">
          Active alerts for your environment. Severest items are listed first.
        </Typography>
        {liveConnected !== null && (
          <Chip
            size="small"
            label={liveConnected ? 'Live updates on' : 'Live updates off'}
            color={liveConnected ? 'success' : 'default'}
            variant={liveConnected ? 'filled' : 'outlined'}
          />
        )}
      </Box>

      {loadError && (
        <MuiAlert
          severity="error"
          sx={{ mb: 3 }}
          action={(
            <Button color="inherit" size="small" onClick={loadAlerts}>
              Retry
            </Button>
          )}
        >
          {loadError}
        </MuiAlert>
      )}

      {loading ? (
        <Paper sx={{ ...panelSx }}>
          <Stack spacing={2}>
            {[1, 2, 3].map((k) => (
              <Skeleton key={k} variant="rounded" height={100} />
            ))}
          </Stack>
        </Paper>
      ) : sortedAlerts.length === 0 && !loadError ? (
        <Paper sx={{ ...panelSx }}>
          <Typography color="text.secondary">No active alerts</Typography>
          <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.5 }}>
            New issues will appear here and via live updates when connected.
          </Typography>
        </Paper>
      ) : sortedAlerts.length > 0 ? (
        <Stack spacing={2}>
          {sortedAlerts.map((alert) => {
            const busy = pendingIds.has(alert.alertId);
            return (
              <Paper key={alert.alertId} sx={{ ...panelSx, p: 2 }}>
                <Box
                  sx={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'flex-start',
                    gap: 2,
                    flexWrap: 'wrap',
                  }}
                >
                  <Box sx={{ flex: 1, minWidth: 200 }}>
                    <Typography variant="h6" sx={{ fontSize: '1.05rem' }}>
                      {alert.message}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                      <Link
                        component={RouterLink}
                        to={`/devices/${alert.deviceId}`}
                        color="inherit"
                        underline="hover"
                      >
                        Device {alert.deviceId}
                      </Link>
                      {alert.sensorId != null && ` • Sensor ${alert.sensorId}`}
                      {alert.triggerValue != null && ` • Value: ${alert.triggerValue}`}
                    </Typography>
                    <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.5 }}>
                      Triggered: {new Date(alert.triggeredAt).toLocaleString()}
                      {alert.acknowledgedAt &&
                        ` • Acknowledged: ${new Date(alert.acknowledgedAt).toLocaleString()}`}
                    </Typography>
                  </Box>
                  <Box
                    sx={{
                      display: 'flex',
                      flexDirection: 'column',
                      gap: 1,
                      alignItems: 'flex-end',
                    }}
                  >
                    <Chip
                      label={alert.severity}
                      color={getSeverityColor(alert.severity)}
                      size="small"
                    />
                    <Stack direction="row" spacing={1} flexWrap="wrap" justifyContent="flex-end">
                      {!alert.acknowledgedAt && (
                        <Button
                          size="small"
                          variant="outlined"
                          disabled={busy}
                          onClick={() => handleAcknowledge(alert.alertId)}
                        >
                          Acknowledge
                        </Button>
                      )}
                      {alert.status === 'Active' && (
                        <Button
                          size="small"
                          variant="contained"
                          color="success"
                          disabled={busy}
                          onClick={() => handleResolve(alert.alertId)}
                        >
                          Resolve
                        </Button>
                      )}
                    </Stack>
                  </Box>
                </Box>
              </Paper>
            );
          })}
        </Stack>
      ) : null}

      <Snackbar
        open={snackbar.open}
        autoHideDuration={6000}
        onClose={() => setSnackbar((s) => ({ ...s, open: false }))}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <MuiAlert
          severity="error"
          variant="filled"
          onClose={() => setSnackbar((s) => ({ ...s, open: false }))}
          sx={{ width: '100%' }}
        >
          {snackbar.message}
        </MuiAlert>
      </Snackbar>
    </Box>
  );
};

export default AlertsPage;
