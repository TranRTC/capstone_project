import React, { useEffect, useState, useCallback } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import {
  Box,
  Typography,
  Paper,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Button,
  Chip,
  Skeleton,
  Alert as MuiAlert,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  Tooltip,
  IconButton,
} from '@mui/material';
import { Refresh as RefreshIcon } from '@mui/icons-material';
import type { ChipProps } from '@mui/material/Chip';
import { apiService } from '../services/api';
import { Actuator, DeviceCommand, DeviceList } from '../types';

const panelSx = {
  p: 2.5,
  border: 1,
  borderColor: 'divider',
  boxShadow: 'none',
  bgcolor: 'background.paper',
};

const STATUSES = ['', 'Pending', 'Sent', 'Acked', 'Failed', 'Timeout'] as const;
const PAGE_SIZE_OPTIONS = [10, 25, 50];

function statusColor(status: string): ChipProps['color'] {
  switch (status) {
    case 'Acked': return 'success';
    case 'Sent': return 'info';
    case 'Pending': return 'default';
    case 'Failed': return 'error';
    case 'Timeout': return 'warning';
    default: return 'default';
  }
}

function formatPayloadSummary(commandType: string, payload: string): string {
  try {
    const p = JSON.parse(payload);
    if (commandType === 'SetPower' && p.on !== undefined) return `on = ${p.on}`;
    if (commandType === 'SetValue' && p.value !== undefined) return `value = ${p.value}`;
    return JSON.stringify(p);
  } catch {
    return payload;
  }
}

function formatDateTime(iso?: string): string {
  if (!iso) return '—';
  return new Date(iso).toLocaleString(undefined, {
    month: 'short', day: 'numeric',
    hour: '2-digit', minute: '2-digit', second: '2-digit',
  });
}

function formatDuration(start?: string, end?: string): string {
  if (!start || !end) return '—';
  const ms = new Date(end).getTime() - new Date(start).getTime();
  if (ms < 1000) return `${ms}ms`;
  return `${(ms / 1000).toFixed(1)}s`;
}

const CommandHistoryPage: React.FC = () => {
  const [searchParams] = useSearchParams();

  const [devices, setDevices] = useState<DeviceList[]>([]);
  const [devicesLoading, setDevicesLoading] = useState(true);
  const [devicesError, setDevicesError] = useState<string | null>(null);
  const [selectedDeviceId, setSelectedDeviceId] = useState<number | ''>('');

  const [actuators, setActuators] = useState<Actuator[]>([]);
  const [selectedActuatorId, setSelectedActuatorId] = useState<number | ''>('');
  const [selectedStatus, setSelectedStatus] = useState<string>('');

  const [commands, setCommands] = useState<DeviceCommand[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(25);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadDevices();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (devicesLoading || devices.length === 0) return;
    const qId = Number.parseInt(searchParams.get('deviceId') ?? '', 10);
    if (Number.isFinite(qId) && devices.some((d) => d.deviceId === qId)) {
      setSelectedDeviceId(qId);
    }
  }, [searchParams, devices, devicesLoading]);

  useEffect(() => {
    setActuators([]);
    setSelectedActuatorId('');
    setCommands([]);
    setTotalCount(0);
    if (selectedDeviceId === '') return;
    loadActuators(selectedDeviceId);
  }, [selectedDeviceId]);

  const loadCommands = useCallback(async () => {
    if (selectedDeviceId === '') return;
    setLoading(true);
    setError(null);
    try {
      const result = await apiService.getDeviceCommands(selectedDeviceId as number, {
        actuatorId: selectedActuatorId !== '' ? (selectedActuatorId as number) : undefined,
        status: selectedStatus || undefined,
        pageNumber: page + 1,
        pageSize,
      });
      setCommands(result.items);
      setTotalCount(result.totalCount);
    } catch (err: any) {
      setError(err?.message || 'Failed to load command history');
      setCommands([]);
    } finally {
      setLoading(false);
    }
  }, [selectedDeviceId, selectedActuatorId, selectedStatus, page, pageSize]);

  useEffect(() => {
    loadCommands();
  }, [loadCommands]);

  const loadDevices = async () => {
    try {
      setDevicesLoading(true);
      setDevicesError(null);
      const data = await apiService.getDevices();
      setDevices(data);
      if (data.length > 0) {
        const qId = Number.parseInt(searchParams.get('deviceId') ?? '', 10);
        if (Number.isFinite(qId) && data.some((d) => d.deviceId === qId)) {
          setSelectedDeviceId(qId);
        } else {
          setSelectedDeviceId(data[0].deviceId);
        }
      }
    } catch (err: any) {
      setDevicesError(err?.message || 'Failed to load devices');
    } finally {
      setDevicesLoading(false);
    }
  };

  const loadActuators = async (deviceId: number) => {
    try {
      const data = await apiService.getActuatorsByDevice(deviceId);
      setActuators(data);
    } catch {
      setActuators([]);
    }
  };

  const handleFilterChange = () => {
    setPage(0);
  };

  const actuatorName = (id?: number) => {
    if (!id) return 'Device-level';
    return actuators.find((a) => a.actuatorId === id)?.name ?? `Actuator ${id}`;
  };

  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom sx={{ mb: 2 }}>
        Command History
      </Typography>

      {devicesError && (
        <MuiAlert severity="error" sx={{ mb: 3 }} action={
          <Button color="inherit" size="small" onClick={loadDevices}>Retry</Button>
        }>
          {devicesError}
        </MuiAlert>
      )}

      {/* Filters */}
      <Paper sx={{ ...panelSx, mb: 3 }}>
        <Typography variant="subtitle2" color="text.secondary" sx={{ mb: 2 }}>
          Filter commands by device, actuator, or status.
        </Typography>

        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems="flex-start" flexWrap="wrap">
          {/* Device */}
          {devicesLoading ? (
            <Skeleton variant="rounded" width={240} height={56} />
          ) : (
            <FormControl sx={{ minWidth: 240 }} disabled={devices.length === 0}>
              <InputLabel id="ch-device-label">Device</InputLabel>
              <Select
                labelId="ch-device-label"
                value={selectedDeviceId}
                label="Device"
                onChange={(e) => { setSelectedDeviceId(e.target.value as number); handleFilterChange(); }}
              >
                {devices.map((d) => (
                  <MenuItem key={d.deviceId} value={d.deviceId}>
                    {d.deviceName}
                    <Typography component="span" variant="caption" color="text.secondary" sx={{ ml: 0.5 }}>
                      ({d.deviceType})
                    </Typography>
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          )}

          {/* Actuator filter */}
          <FormControl sx={{ minWidth: 200 }} disabled={selectedDeviceId === ''}>
            <InputLabel id="ch-actuator-label">Actuator</InputLabel>
            <Select
              labelId="ch-actuator-label"
              value={selectedActuatorId}
              label="Actuator"
              onChange={(e) => { setSelectedActuatorId(e.target.value as number | ''); handleFilterChange(); }}
            >
              <MenuItem value="">All actuators</MenuItem>
              {actuators.map((a) => (
                <MenuItem key={a.actuatorId} value={a.actuatorId}>
                  {a.name}
                  <Typography component="span" variant="caption" color="text.secondary" sx={{ ml: 0.5 }}>
                    ({a.kind})
                  </Typography>
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          {/* Status filter */}
          <FormControl sx={{ minWidth: 160 }}>
            <InputLabel id="ch-status-label">Status</InputLabel>
            <Select
              labelId="ch-status-label"
              value={selectedStatus}
              label="Status"
              onChange={(e) => { setSelectedStatus(e.target.value); handleFilterChange(); }}
            >
              {STATUSES.map((s) => (
                <MenuItem key={s} value={s}>{s || 'All statuses'}</MenuItem>
              ))}
            </Select>
          </FormControl>

          <Tooltip title="Refresh">
            <span>
              <IconButton
                onClick={loadCommands}
                disabled={loading || selectedDeviceId === ''}
                sx={{ mt: 0.75 }}
              >
                <RefreshIcon />
              </IconButton>
            </span>
          </Tooltip>
        </Stack>

        {/* Actuator last-known state summary */}
        {selectedActuatorId !== '' && (() => {
          const act = actuators.find((a) => a.actuatorId === selectedActuatorId);
          if (!act?.lastKnownState) return null;
          return (
            <Box sx={{ mt: 2, display: 'flex', alignItems: 'center', gap: 1 }}>
              <Typography variant="body2" color="text.secondary">
                Last confirmed state of <strong>{act.name}</strong>:
              </Typography>
              <Chip
                label={act.lastKnownState}
                color={act.lastKnownState === 'on' ? 'success' : act.lastKnownState === 'off' ? 'default' : 'info'}
                size="small"
              />
              {act.lastStateAt && (
                <Typography variant="caption" color="text.secondary">
                  as of {formatDateTime(act.lastStateAt)}
                </Typography>
              )}
            </Box>
          );
        })()}
      </Paper>

      {/* Error */}
      {error && (
        <MuiAlert severity="error" sx={{ mb: 3 }} action={
          <Button color="inherit" size="small" onClick={loadCommands}>Retry</Button>
        }>
          {error}
        </MuiAlert>
      )}

      {/* Table */}
      {selectedDeviceId === '' ? (
        <Paper sx={{ ...panelSx }}>
          <Typography color="text.secondary">Select a device to view its command history.</Typography>
        </Paper>
      ) : loading && commands.length === 0 ? (
        <Paper sx={{ ...panelSx }}>
          <Stack spacing={1.5}>
            {[1, 2, 3, 4, 5].map((k) => <Skeleton key={k} variant="rounded" height={48} />)}
          </Stack>
        </Paper>
      ) : !loading && commands.length === 0 && !error ? (
        <Paper sx={{ ...panelSx }}>
          <Typography color="text.secondary">No commands found for the selected filters.</Typography>
          <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.5 }}>
            Commands are created when you control an actuator from the{' '}
            <Link to={`/devices/${selectedDeviceId}`}>device detail page</Link>.
          </Typography>
        </Paper>
      ) : commands.length > 0 ? (
        <Paper sx={{ ...panelSx, p: 0 }}>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>ID</TableCell>
                  <TableCell>Actuator</TableCell>
                  <TableCell>Type</TableCell>
                  <TableCell>Payload</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Sent At</TableCell>
                  <TableCell>Duration</TableCell>
                  <TableCell>Error</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {commands.map((cmd) => (
                  <TableRow key={cmd.commandId} hover>
                    <TableCell>
                      <Typography variant="caption" color="text.secondary">#{cmd.commandId}</Typography>
                      <Typography variant="caption" color="text.disabled" display="block">
                        {formatDateTime(cmd.createdAt)}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">{actuatorName(cmd.actuatorId)}</Typography>
                    </TableCell>
                    <TableCell>
                      <Chip label={cmd.commandType} size="small" variant="outlined" />
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2" sx={{ fontFamily: 'monospace', fontSize: '0.78rem' }}>
                        {formatPayloadSummary(cmd.commandType, cmd.payload)}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={cmd.status}
                        color={statusColor(cmd.status)}
                        size="small"
                        variant={cmd.status === 'Acked' ? 'filled' : 'outlined'}
                      />
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">{formatDateTime(cmd.sentAt)}</Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">
                        {formatDuration(cmd.createdAt, cmd.completedAt)}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      {cmd.errorMessage ? (
                        <Tooltip title={cmd.errorMessage}>
                          <Typography
                            variant="caption"
                            color="error"
                            sx={{ maxWidth: 180, display: 'block', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}
                          >
                            {cmd.errorMessage}
                          </Typography>
                        </Tooltip>
                      ) : (
                        <Typography variant="caption" color="text.disabled">—</Typography>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          <TablePagination
            component="div"
            count={totalCount}
            page={page}
            onPageChange={(_, newPage) => setPage(newPage)}
            rowsPerPage={pageSize}
            onRowsPerPageChange={(e) => { setPageSize(Number(e.target.value)); setPage(0); }}
            rowsPerPageOptions={PAGE_SIZE_OPTIONS}
          />
        </Paper>
      ) : null}
    </Box>
  );
};

export default CommandHistoryPage;
