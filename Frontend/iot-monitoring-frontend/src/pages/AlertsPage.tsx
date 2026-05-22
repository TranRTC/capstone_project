import React, { useCallback, useEffect, useMemo, useState } from 'react';
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
  Tabs,
  Tab,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  IconButton,
} from '@mui/material';
import { DeleteOutline as DeleteIcon } from '@mui/icons-material';
import type { ChipProps } from '@mui/material/Chip';
import { apiService } from '../services/api';
import { authService } from '../services/authService';
import { signalRService } from '../services/signalRService';
import ConfirmDialog from '../components/common/ConfirmDialog';
import { Alert as AlertDto, DeviceList } from '../types';

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

const HISTORY_STATUSES = ['', 'Resolved', 'Acknowledged', 'Active'] as const;
const HISTORY_SEVERITIES = ['', 'Critical', 'High', 'Medium', 'Low'] as const;
const PAGE_SIZE_OPTIONS = [10, 25, 50];

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

function formatDateTime(iso?: string): string {
  if (!iso) return '—';
  return new Date(iso).toLocaleString();
}

const AlertsPage: React.FC = () => {
  const canWrite = authService.isOperatorOrAbove();
  const [tab, setTab] = useState(0);

  const [activeAlerts, setActiveAlerts] = useState<AlertDto[]>([]);
  const [activeLoading, setActiveLoading] = useState(true);
  const [activeLoadError, setActiveLoadError] = useState<string | null>(null);
  const [liveConnected, setLiveConnected] = useState<boolean | null>(null);
  const [pendingIds, setPendingIds] = useState<Set<number>>(() => new Set());

  const [devices, setDevices] = useState<DeviceList[]>([]);
  const [historyStatus, setHistoryStatus] = useState<string>('Resolved');
  const [historySeverity, setHistorySeverity] = useState<string>('');
  const [historyDeviceId, setHistoryDeviceId] = useState<number | ''>('');
  const [historyItems, setHistoryItems] = useState<AlertDto[]>([]);
  const [historyTotal, setHistoryTotal] = useState(0);
  const [historyPage, setHistoryPage] = useState(0);
  const [historyPageSize, setHistoryPageSize] = useState(25);
  const [historyLoading, setHistoryLoading] = useState(false);
  const [historyError, setHistoryError] = useState<string | null>(null);

  const [snackbar, setSnackbar] = useState<{ open: boolean; message: string }>({
    open: false,
    message: '',
  });

  const [confirmDeleteAlert, setConfirmDeleteAlert] = useState<AlertDto | null>(null);
  const [confirmBulkMode, setConfirmBulkMode] = useState<'matching' | 'allForDevice' | null>(null);
  const [deleteLoading, setDeleteLoading] = useState(false);

  const sortedActiveAlerts = useMemo(() => sortAlerts(activeAlerts), [activeAlerts]);

  const loadActiveAlerts = async () => {
    try {
      setActiveLoading(true);
      setActiveLoadError(null);
      const data = await apiService.getActiveAlerts();
      setActiveAlerts(data);
    } catch (err: any) {
      console.error('Error loading alerts:', err);
      setActiveLoadError(err?.message || 'Failed to load alerts');
      setActiveAlerts([]);
    } finally {
      setActiveLoading(false);
    }
  };

  const loadHistory = useCallback(async () => {
    setHistoryLoading(true);
    setHistoryError(null);
    try {
      const result = await apiService.getAlerts({
        status: historyStatus || undefined,
        severity: historySeverity || undefined,
        deviceId: historyDeviceId !== '' ? (historyDeviceId as number) : undefined,
        pageNumber: historyPage + 1,
        pageSize: historyPageSize,
      });
      setHistoryItems(result.items);
      setHistoryTotal(result.totalCount);
    } catch (err: any) {
      setHistoryError(err?.message || 'Failed to load alert history');
      setHistoryItems([]);
      setHistoryTotal(0);
    } finally {
      setHistoryLoading(false);
    }
  }, [historyStatus, historySeverity, historyDeviceId, historyPage, historyPageSize]);

  useEffect(() => {
    loadActiveAlerts();
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

  useEffect(() => {
    apiService.getDevices().then(setDevices).catch(() => setDevices([]));
  }, []);

  useEffect(() => {
    if (tab === 1) {
      loadHistory();
    }
  }, [tab, loadHistory]);

  const connectSignalR = async () => {
    try {
      await signalRService.start();
      await signalRService.subscribeToAlerts();

      signalRService.onNewAlert((alert) => {
        setActiveAlerts((prev) => {
          if (prev.some((a) => a.alertId === alert.alertId)) return prev;
          return [alert, ...prev];
        });
      });

      signalRService.onAlertAcknowledged((updated) => {
        setActiveAlerts((prev) =>
          prev.map((a) => (a.alertId === updated.alertId ? { ...a, ...updated } : a))
        );
      });

      signalRService.onAlertResolved((resolved) => {
        setActiveAlerts((prev) => prev.filter((a) => a.alertId !== resolved.alertId));
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
      await loadActiveAlerts();
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
      await loadActiveAlerts();
    } catch (err: any) {
      console.error('Error resolving alert:', err);
      showActionError(err?.message || 'Failed to resolve alert');
    } finally {
      setPending(alertId, false);
    }
  };

  const doDeleteSingle = async () => {
    if (!confirmDeleteAlert) return;
    setDeleteLoading(true);
    try {
      await apiService.deleteAlert(confirmDeleteAlert.alertId);
      setConfirmDeleteAlert(null);
      if (tab === 0) {
        await loadActiveAlerts();
      } else {
        await loadHistory();
      }
    } catch (err: any) {
      showActionError(err?.message || 'Failed to delete alert');
      setConfirmDeleteAlert(null);
    } finally {
      setDeleteLoading(false);
    }
  };

  const doDeleteBulk = async () => {
    if (historyDeviceId === '' || !confirmBulkMode) return;
    setDeleteLoading(true);
    try {
      const params: { deviceId: number; status?: string; severity?: string } = {
        deviceId: historyDeviceId as number,
      };
      if (confirmBulkMode === 'matching') {
        if (historyStatus) params.status = historyStatus;
        if (historySeverity) params.severity = historySeverity;
      }
      const result = await apiService.deleteAlertsBulk(params);
      setConfirmBulkMode(null);
      setSnackbar({
        open: true,
        message: `Deleted ${result.deletedCount} alert(s).`,
      });
      await loadHistory();
    } catch (err: any) {
      showActionError(err?.message || 'Failed to delete alerts');
      setConfirmBulkMode(null);
    } finally {
      setDeleteLoading(false);
    }
  };

  const handleHistoryFilterChange = () => {
    setHistoryPage(0);
  };

  const deviceLabel = (deviceId: number) => {
    const d = devices.find((x) => x.deviceId === deviceId);
    return d ? d.deviceName : `Device ${deviceId}`;
  };

  const selectedHistoryDeviceName =
    historyDeviceId !== ''
      ? deviceLabel(historyDeviceId as number)
      : '';

  const bulkMatchingMessage = () => {
    const parts = [`Delete alerts for "${selectedHistoryDeviceName}"`];
    if (historyStatus) parts.push(`with status "${historyStatus}"`);
    if (historySeverity) parts.push(`and severity "${historySeverity}"`);
    parts.push('? This may remove more rows than shown on the current page. This cannot be undone.');
    return parts.join(' ');
  };

  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom sx={{ mb: 2 }}>
        Alerts
      </Typography>

      <ConfirmDialog
        open={confirmDeleteAlert != null}
        title="Delete Alert"
        message={
          confirmDeleteAlert
            ? `Delete this alert permanently? "${confirmDeleteAlert.message.slice(0, 80)}${confirmDeleteAlert.message.length > 80 ? '…' : ''}"`
            : ''
        }
        confirmLabel="Delete"
        loading={deleteLoading}
        onConfirm={doDeleteSingle}
        onCancel={() => setConfirmDeleteAlert(null)}
      />

      <ConfirmDialog
        open={confirmBulkMode === 'matching'}
        title="Delete Matching Alerts"
        message={bulkMatchingMessage()}
        confirmLabel="Delete"
        loading={deleteLoading}
        onConfirm={doDeleteBulk}
        onCancel={() => setConfirmBulkMode(null)}
      />

      <ConfirmDialog
        open={confirmBulkMode === 'allForDevice'}
        title="Delete All Alerts for Device"
        message={`Delete all alerts for "${selectedHistoryDeviceName}" (any status)? This cannot be undone.`}
        confirmLabel="Delete All"
        loading={deleteLoading}
        onConfirm={doDeleteBulk}
        onCancel={() => setConfirmBulkMode(null)}
      />

      <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ mb: 2 }}>
        <Tab label="Active" />
        <Tab label="History" />
      </Tabs>

      {tab === 0 && (
        <>
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
            {!canWrite && (
              <Chip size="small" label="View only" variant="outlined" />
            )}
          </Box>

          {activeLoadError && (
            <MuiAlert
              severity="error"
              sx={{ mb: 3 }}
              action={(
                <Button color="inherit" size="small" onClick={loadActiveAlerts}>
                  Retry
                </Button>
              )}
            >
              {activeLoadError}
            </MuiAlert>
          )}

          {activeLoading ? (
            <Paper sx={{ ...panelSx }}>
              <Stack spacing={2}>
                {[1, 2, 3].map((k) => (
                  <Skeleton key={k} variant="rounded" height={100} />
                ))}
              </Stack>
            </Paper>
          ) : sortedActiveAlerts.length === 0 && !activeLoadError ? (
            <Paper sx={{ ...panelSx }}>
              <Typography color="text.secondary">No active alerts</Typography>
              <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.5 }}>
                New issues will appear here and via live updates when connected.
              </Typography>
            </Paper>
          ) : sortedActiveAlerts.length > 0 ? (
            <Stack spacing={2}>
              {sortedActiveAlerts.map((alert) => {
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
                            {deviceLabel(alert.deviceId)}
                          </Link>
                          {alert.sensorId != null && ` • Sensor ${alert.sensorId}`}
                          {alert.triggerValue != null && ` • Value: ${alert.triggerValue}`}
                        </Typography>
                        <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.5 }}>
                          Triggered: {formatDateTime(alert.triggeredAt)}
                          {alert.acknowledgedAt &&
                            ` • Acknowledged: ${formatDateTime(alert.acknowledgedAt)}`}
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
                        {canWrite && (
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
                            <Button
                              size="small"
                              variant="outlined"
                              color="error"
                              disabled={busy || deleteLoading}
                              onClick={() => setConfirmDeleteAlert(alert)}
                            >
                              Delete
                            </Button>
                          </Stack>
                        )}
                      </Box>
                    </Box>
                  </Paper>
                );
              })}
            </Stack>
          ) : null}
        </>
      )}

      {tab === 1 && (
        <>
          <Paper sx={{ ...panelSx, mb: 3 }}>
            <Typography variant="subtitle2" color="text.secondary" sx={{ mb: 2 }}>
              Browse past alerts. Use filters to narrow results.
            </Typography>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} flexWrap="wrap">
              <FormControl sx={{ minWidth: 160 }} size="small">
                <InputLabel id="hist-status-label">Status</InputLabel>
                <Select
                  labelId="hist-status-label"
                  value={historyStatus}
                  label="Status"
                  onChange={(e) => {
                    setHistoryStatus(e.target.value);
                    handleHistoryFilterChange();
                  }}
                >
                  {HISTORY_STATUSES.map((s) => (
                    <MenuItem key={s || 'all'} value={s}>
                      {s || 'All statuses'}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <FormControl sx={{ minWidth: 140 }} size="small">
                <InputLabel id="hist-sev-label">Severity</InputLabel>
                <Select
                  labelId="hist-sev-label"
                  value={historySeverity}
                  label="Severity"
                  onChange={(e) => {
                    setHistorySeverity(e.target.value);
                    handleHistoryFilterChange();
                  }}
                >
                  {HISTORY_SEVERITIES.map((s) => (
                    <MenuItem key={s || 'all'} value={s}>
                      {s || 'All severities'}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <FormControl sx={{ minWidth: 220 }} size="small">
                <InputLabel id="hist-device-label">Device</InputLabel>
                <Select
                  labelId="hist-device-label"
                  value={historyDeviceId}
                  label="Device"
                  onChange={(e) => {
                    const v = e.target.value;
                    setHistoryDeviceId(v === '' ? '' : (v as number));
                    handleHistoryFilterChange();
                  }}
                >
                  <MenuItem value="">All devices</MenuItem>
                  {devices.map((d) => (
                    <MenuItem key={d.deviceId} value={d.deviceId}>
                      {d.deviceName}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <Button
                variant="outlined"
                size="small"
                onClick={() => loadHistory()}
                disabled={historyLoading}
                sx={{ alignSelf: 'center' }}
              >
                Refresh
              </Button>
              {canWrite && historyDeviceId !== '' && (
                <>
                  <Button
                    variant="outlined"
                    size="small"
                    color="error"
                    disabled={historyLoading || deleteLoading}
                    onClick={() => setConfirmBulkMode('matching')}
                    sx={{ alignSelf: 'center' }}
                  >
                    Delete matching
                  </Button>
                  <Button
                    variant="outlined"
                    size="small"
                    color="error"
                    disabled={historyLoading || deleteLoading}
                    onClick={() => setConfirmBulkMode('allForDevice')}
                    sx={{ alignSelf: 'center' }}
                  >
                    Delete all for device
                  </Button>
                </>
              )}
            </Stack>
          </Paper>

          {historyError && (
            <MuiAlert
              severity="error"
              sx={{ mb: 3 }}
              action={(
                <Button color="inherit" size="small" onClick={() => loadHistory()}>
                  Retry
                </Button>
              )}
            >
              {historyError}
            </MuiAlert>
          )}

          <TableContainer component={Paper} sx={{ ...panelSx, p: 0 }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Message</TableCell>
                  <TableCell>Severity</TableCell>
                  <TableCell>Device</TableCell>
                  <TableCell>Sensor</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Triggered</TableCell>
                  <TableCell>Acknowledged</TableCell>
                  <TableCell>Resolved</TableCell>
                  {canWrite && <TableCell align="right">Actions</TableCell>}
                </TableRow>
              </TableHead>
              <TableBody>
                {historyLoading ? (
                  <TableRow>
                    <TableCell colSpan={canWrite ? 9 : 8}>
                      <Stack spacing={1} sx={{ py: 2 }}>
                        {[1, 2, 3].map((k) => (
                          <Skeleton key={k} variant="rounded" height={40} />
                        ))}
                      </Stack>
                    </TableCell>
                  </TableRow>
                ) : historyItems.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={canWrite ? 9 : 8}>
                      <Typography color="text.secondary" sx={{ py: 2 }}>
                        No alerts match these filters.
                      </Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  historyItems.map((alert) => (
                    <TableRow key={alert.alertId} hover>
                      <TableCell>
                        <Typography variant="body2">{alert.message}</Typography>
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={alert.severity}
                          color={getSeverityColor(alert.severity)}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>
                        <Link
                          component={RouterLink}
                          to={`/devices/${alert.deviceId}`}
                          underline="hover"
                        >
                          {deviceLabel(alert.deviceId)}
                        </Link>
                      </TableCell>
                      <TableCell>{alert.sensorId ?? '—'}</TableCell>
                      <TableCell>
                        <Chip label={alert.status} size="small" variant="outlined" />
                      </TableCell>
                      <TableCell>{formatDateTime(alert.triggeredAt)}</TableCell>
                      <TableCell>{formatDateTime(alert.acknowledgedAt)}</TableCell>
                      <TableCell>{formatDateTime(alert.resolvedAt)}</TableCell>
                      {canWrite && (
                        <TableCell align="right">
                          <IconButton
                            size="small"
                            color="error"
                            title="Delete"
                            disabled={deleteLoading}
                            onClick={() => setConfirmDeleteAlert(alert)}
                          >
                            <DeleteIcon fontSize="small" />
                          </IconButton>
                        </TableCell>
                      )}
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
            <TablePagination
              component="div"
              count={historyTotal}
              page={historyPage}
              onPageChange={(_, p) => setHistoryPage(p)}
              rowsPerPage={historyPageSize}
              onRowsPerPageChange={(e) => {
                setHistoryPageSize(parseInt(e.target.value, 10));
                setHistoryPage(0);
              }}
              rowsPerPageOptions={PAGE_SIZE_OPTIONS}
            />
          </TableContainer>
        </>
      )}

      <Snackbar
        open={snackbar.open}
        autoHideDuration={6000}
        onClose={() => setSnackbar((s) => ({ ...s, open: false }))}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <MuiAlert
          severity={snackbar.message.startsWith('Deleted') ? 'success' : 'error'}
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
