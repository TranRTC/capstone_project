import React, { useEffect, useMemo, useState } from 'react';
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
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Switch,
  FormControlLabel,
  IconButton,
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  DeleteOutline as DeleteIcon,
  Visibility as ViewIcon,
} from '@mui/icons-material';
import type { ChipProps } from '@mui/material/Chip';
import { apiService } from '../services/api';
import { authService } from '../services/authService';
import ConfirmDialog from '../components/common/ConfirmDialog';
import { AlertRule, CreateAlertRule, DeviceList, Sensor } from '../types';

const panelSx = {
  p: 2.5,
  border: 1,
  borderColor: 'divider',
  boxShadow: 'none',
  bgcolor: 'background.paper',
};

const RULE_TYPES = ['threshold', 'range', 'change'] as const;
const SEVERITIES = ['Low', 'Medium', 'High', 'Critical'] as const;
const OPERATORS = ['>', '>=', '<', '<=', '==', '!='] as const;

function getSeverityColor(severity: string): ChipProps['color'] {
  switch (severity.toLowerCase()) {
    case 'critical': return 'error';
    case 'high': return 'warning';
    case 'medium': return 'info';
    default: return 'default';
  }
}

function ruleConditionSummary(rule: AlertRule): string {
  switch (rule.ruleType.toLowerCase()) {
    case 'threshold':
      return `Value ${rule.comparisonOperator ?? '>'} ${rule.thresholdValue ?? '—'}`;
    case 'range':
      return `Outside ${rule.minValue ?? '—'} – ${rule.maxValue ?? '—'}`;
    case 'change':
      return 'Any state change';
    default:
      return rule.condition;
  }
}

interface RuleFormState {
  ruleName: string;
  ruleType: 'threshold' | 'range' | 'change';
  condition: string;
  severity: string;
  isEnabled: boolean;
  thresholdValue: string;
  comparisonOperator: string;
  minValue: string;
  maxValue: string;
}

const emptyForm = (): RuleFormState => ({
  ruleName: '',
  ruleType: 'threshold',
  condition: '',
  severity: 'Medium',
  isEnabled: true,
  thresholdValue: '',
  comparisonOperator: '>',
  minValue: '',
  maxValue: '',
});

function ruleToForm(rule: AlertRule): RuleFormState {
  return {
    ruleName: rule.ruleName,
    ruleType: (rule.ruleType.toLowerCase() as RuleFormState['ruleType']) ?? 'threshold',
    condition: rule.condition,
    severity: rule.severity,
    isEnabled: rule.isEnabled,
    thresholdValue: rule.thresholdValue != null ? String(rule.thresholdValue) : '',
    comparisonOperator: rule.comparisonOperator ?? '>',
    minValue: rule.minValue != null ? String(rule.minValue) : '',
    maxValue: rule.maxValue != null ? String(rule.maxValue) : '',
  };
}

function formToPayload(form: RuleFormState, deviceId: number, sensorId: number): CreateAlertRule {
  const base: CreateAlertRule = {
    deviceId,
    sensorId,
    ruleName: form.ruleName.trim(),
    ruleType: form.ruleType,
    condition: form.condition.trim(),
    severity: form.severity,
    isEnabled: form.isEnabled,
  };

  if (form.ruleType === 'threshold') {
    base.thresholdValue = form.thresholdValue !== '' ? Number(form.thresholdValue) : undefined;
    base.comparisonOperator = form.comparisonOperator;
  } else if (form.ruleType === 'range') {
    base.minValue = form.minValue !== '' ? Number(form.minValue) : undefined;
    base.maxValue = form.maxValue !== '' ? Number(form.maxValue) : undefined;
  }

  return base;
}

function formatDateTime(iso?: string): string {
  if (!iso) return '—';
  return new Date(iso).toLocaleString();
}

const AlertRulesPage: React.FC = () => {
  const [searchParams] = useSearchParams();

  const [devices, setDevices] = useState<DeviceList[]>([]);
  const [devicesLoading, setDevicesLoading] = useState(true);
  const [devicesError, setDevicesError] = useState<string | null>(null);
  const [selectedDeviceId, setSelectedDeviceId] = useState<number | ''>('');
  const [selectedSensorId, setSelectedSensorId] = useState<number | ''>('');

  const [filterSensors, setFilterSensors] = useState<Sensor[]>([]);
  const [filterSensorsLoading, setFilterSensorsLoading] = useState(false);

  const [allRules, setAllRules] = useState<AlertRule[]>([]);
  const [rulesLoading, setRulesLoading] = useState(false);
  const [rulesError, setRulesError] = useState<string | null>(null);
  const [sensorNameById, setSensorNameById] = useState<Record<number, string>>({});

  const [formOpen, setFormOpen] = useState(false);
  const [editingRule, setEditingRule] = useState<AlertRule | null>(null);
  const [form, setForm] = useState<RuleFormState>(emptyForm());
  const [formError, setFormError] = useState<string | null>(null);
  const [formSaving, setFormSaving] = useState(false);
  const [dialogDeviceId, setDialogDeviceId] = useState<number | ''>('');
  const [dialogSensorId, setDialogSensorId] = useState<number | ''>('');
  const [dialogSensors, setDialogSensors] = useState<Sensor[]>([]);
  const [dialogSensorsLoading, setDialogSensorsLoading] = useState(false);

  const [viewingRule, setViewingRule] = useState<AlertRule | null>(null);
  const [confirmRule, setConfirmRule] = useState<AlertRule | null>(null);
  const [deleteLoading, setDeleteLoading] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const canWrite = authService.isOperatorOrAbove();

  const deviceNameById = useMemo(() => {
    const map: Record<number, string> = {};
    devices.forEach((d) => { map[d.deviceId] = d.deviceName; });
    return map;
  }, [devices]);

  const filteredRules = useMemo(() => {
    let list = allRules;
    if (selectedDeviceId !== '') {
      list = list.filter((r) => r.deviceId === selectedDeviceId);
    }
    if (selectedSensorId !== '') {
      list = list.filter((r) => r.sensorId === selectedSensorId);
    }
    return list;
  }, [allRules, selectedDeviceId, selectedSensorId]);

  const needsDialogDeviceSensor =
    formOpen && !editingRule && (selectedDeviceId === '' || selectedSensorId === '');

  const formContextSensor = useMemo(() => {
    const targetSensorId = editingRule?.sensorId ?? (selectedSensorId !== '' ? selectedSensorId : dialogSensorId);
    if (targetSensorId === '' || targetSensorId === undefined) return undefined;
    const sid = targetSensorId as number;
    return (
      dialogSensors.find((s) => s.sensorId === sid)
      ?? filterSensors.find((s) => s.sensorId === sid)
    );
  }, [editingRule, selectedSensorId, filterSensors, dialogSensorId, dialogSensors]);

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
    if (!devicesLoading) {
      loadAllRules();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [devicesLoading]);

  useEffect(() => {
    setSelectedSensorId('');
    if (selectedDeviceId === '') {
      setFilterSensors([]);
      return;
    }
    loadFilterSensors(selectedDeviceId);
  }, [selectedDeviceId]);

  useEffect(() => {
    if (!formOpen || editingRule || !needsDialogDeviceSensor) return;
    if (dialogDeviceId === '') {
      setDialogSensors([]);
      setDialogSensorId('');
      return;
    }
    loadDialogSensors(dialogDeviceId);
  }, [formOpen, editingRule, needsDialogDeviceSensor, dialogDeviceId]);

  const refreshSensorNames = async (rules: AlertRule[]) => {
    const deviceIds = Array.from(
      new Set(rules.map((r) => r.deviceId).filter((id): id is number => id != null))
    );
    if (deviceIds.length === 0) {
      setSensorNameById({});
      return;
    }
    try {
      const sensorLists = await Promise.all(deviceIds.map((id) => apiService.getSensorsByDevice(id)));
      const map: Record<number, string> = {};
      sensorLists.flat().forEach((s) => { map[s.sensorId] = s.sensorName; });
      setSensorNameById(map);
    } catch {
      // keep previous cache on failure
    }
  };

  const loadAllRules = async () => {
    try {
      setRulesLoading(true);
      setRulesError(null);
      const data = await apiService.getAlertRules();
      setAllRules(data);
      await refreshSensorNames(data);
    } catch (err: any) {
      setRulesError(err?.message || 'Failed to load alert rules');
      setAllRules([]);
    } finally {
      setRulesLoading(false);
    }
  };

  const loadDevices = async () => {
    try {
      setDevicesLoading(true);
      setDevicesError(null);
      const data = await apiService.getDevices();
      setDevices(data);
    } catch (err: any) {
      setDevicesError(err?.message || 'Failed to load devices');
    } finally {
      setDevicesLoading(false);
    }
  };

  const loadFilterSensors = async (deviceId: number) => {
    try {
      setFilterSensorsLoading(true);
      const data = await apiService.getSensorsByDevice(deviceId);
      setFilterSensors(data);
    } catch {
      setFilterSensors([]);
    } finally {
      setFilterSensorsLoading(false);
    }
  };

  const loadDialogSensors = async (deviceId: number) => {
    try {
      setDialogSensorsLoading(true);
      const data = await apiService.getSensorsByDevice(deviceId);
      setDialogSensors(data);
    } catch {
      setDialogSensors([]);
    } finally {
      setDialogSensorsLoading(false);
    }
  };

  const resolveSaveIds = (): { deviceId: number; sensorId: number } | null => {
    if (editingRule) {
      if (editingRule.deviceId == null || editingRule.sensorId == null) return null;
      return { deviceId: editingRule.deviceId, sensorId: editingRule.sensorId };
    }
    if (selectedDeviceId !== '' && selectedSensorId !== '') {
      return { deviceId: selectedDeviceId as number, sensorId: selectedSensorId as number };
    }
    if (dialogDeviceId !== '' && dialogSensorId !== '') {
      return { deviceId: dialogDeviceId as number, sensorId: dialogSensorId as number };
    }
    return null;
  };

  const openCreate = () => {
    setEditingRule(null);
    setForm(emptyForm());
    setFormError(null);
    if (selectedDeviceId !== '' && selectedSensorId !== '') {
      setDialogDeviceId(selectedDeviceId);
      setDialogSensorId(selectedSensorId);
      loadDialogSensors(selectedDeviceId as number);
    } else {
      setDialogDeviceId('');
      setDialogSensorId('');
    }
    setFormOpen(true);
  };

  const openEdit = (rule: AlertRule) => {
    setEditingRule(rule);
    setForm(ruleToForm(rule));
    setFormError(null);
    setDialogDeviceId(rule.deviceId ?? '');
    setDialogSensorId(rule.sensorId ?? '');
    if (rule.deviceId != null) {
      loadDialogSensors(rule.deviceId);
    }
    setFormOpen(true);
  };

  const openView = (rule: AlertRule) => {
    setViewingRule(rule);
  };

  const closeForm = () => {
    setFormOpen(false);
    setEditingRule(null);
    setDialogDeviceId('');
    setDialogSensorId('');
    setDialogSensors([]);
  };

  const handleFormChange = <K extends keyof RuleFormState>(key: K, value: RuleFormState[K]) => {
    setForm((prev) => ({ ...prev, [key]: value }));
  };

  const handleSave = async () => {
    if (!form.ruleName.trim()) { setFormError('Rule Name is required.'); return; }
    if (!form.condition.trim()) { setFormError('Condition / Message is required.'); return; }
    if (form.ruleType === 'threshold' && form.thresholdValue === '') {
      setFormError('Threshold Value is required for threshold rules.'); return;
    }
    if (form.ruleType === 'range' && (form.minValue === '' || form.maxValue === '')) {
      setFormError('Min Value and Max Value are required for range rules.'); return;
    }

    const ids = resolveSaveIds();
    if (!ids) {
      setFormError('Device and sensor are required.');
      return;
    }

    setFormSaving(true);
    setFormError(null);
    try {
      const payload = formToPayload(form, ids.deviceId, ids.sensorId);
      if (editingRule) {
        await apiService.updateAlertRule(editingRule.alertRuleId, payload);
      } else {
        await apiService.createAlertRule(payload);
      }
      closeForm();
      await loadAllRules();
    } catch (err: any) {
      setFormError(err?.message || 'Failed to save rule.');
    } finally {
      setFormSaving(false);
    }
  };

  const handleDelete = (rule: AlertRule) => {
    setActionError(null);
    setConfirmRule(rule);
  };

  const doDeleteRule = async () => {
    if (!confirmRule) return;
    setDeleteLoading(true);
    try {
      await apiService.deleteAlertRule(confirmRule.alertRuleId);
      setConfirmRule(null);
      await loadAllRules();
    } catch (err: any) {
      setActionError(err?.message || 'Failed to delete rule.');
      setConfirmRule(null);
    } finally {
      setDeleteLoading(false);
    }
  };

  const clearFilters = () => {
    setSelectedDeviceId('');
    setSelectedSensorId('');
    setFilterSensors([]);
  };

  const deviceLabel = (rule: AlertRule) => {
    if (rule.deviceId == null) return '—';
    return deviceNameById[rule.deviceId] ?? `Device ${rule.deviceId}`;
  };

  const sensorLabel = (rule: AlertRule) => {
    if (rule.sensorId == null) return '—';
    return sensorNameById[rule.sensorId] ?? `Sensor ${rule.sensorId}`;
  };

  const hasFilters = selectedDeviceId !== '' || selectedSensorId !== '';

  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom sx={{ mb: 2 }}>
        Alert Rules
      </Typography>

      <ConfirmDialog
        open={confirmRule != null}
        title="Delete Alert Rule"
        message={`Delete rule "${confirmRule?.ruleName}"? This cannot be undone.`}
        confirmLabel="Delete"
        loading={deleteLoading}
        onConfirm={doDeleteRule}
        onCancel={() => setConfirmRule(null)}
      />

      {actionError && (
        <MuiAlert severity="error" sx={{ mb: 2 }} onClose={() => setActionError(null)}>
          {actionError}
        </MuiAlert>
      )}

      {devicesError && (
        <MuiAlert severity="error" sx={{ mb: 3 }} action={<Button color="inherit" size="small" onClick={loadDevices}>Retry</Button>}>
          {devicesError}
        </MuiAlert>
      )}

      <Paper sx={{ ...panelSx, mb: 3 }}>
        <Typography variant="subtitle2" color="text.secondary" sx={{ mb: 2 }}>
          All alert rules in your environment. Optionally filter by device or sensor.
        </Typography>

        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems="flex-start" flexWrap="wrap">
          {devicesLoading ? (
            <Skeleton variant="rounded" width={280} height={56} />
          ) : (
            <FormControl sx={{ minWidth: 280 }} disabled={devices.length === 0} size="small">
              <InputLabel id="ar-device-label">Filter by device</InputLabel>
              <Select
                labelId="ar-device-label"
                value={selectedDeviceId}
                label="Filter by device"
                onChange={(e) => {
                  const v = e.target.value;
                  setSelectedDeviceId(v === '' ? '' : (v as number));
                }}
              >
                <MenuItem value="">All devices</MenuItem>
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

          {filterSensorsLoading ? (
            <Skeleton variant="rounded" width={280} height={56} />
          ) : (
            <FormControl
              sx={{ minWidth: 280 }}
              disabled={selectedDeviceId === ''}
              size="small"
            >
              <InputLabel id="ar-sensor-label">Filter by sensor</InputLabel>
              <Select
                labelId="ar-sensor-label"
                value={selectedSensorId}
                label="Filter by sensor"
                onChange={(e) => {
                  const v = e.target.value;
                  setSelectedSensorId(v === '' ? '' : (v as number));
                }}
              >
                <MenuItem value="">All sensors</MenuItem>
                {filterSensors.map((s) => (
                  <MenuItem key={s.sensorId} value={s.sensorId}>
                    {s.sensorName}
                    <Typography component="span" variant="caption" color="text.secondary" sx={{ ml: 0.5 }}>
                      ({s.sensorType}{s.unit ? `, ${s.unit}` : ''})
                    </Typography>
                  </MenuItem>
                ))}
              </Select>
              {selectedDeviceId !== '' && filterSensors.length === 0 && !filterSensorsLoading && (
                <FormHelperText>No sensors on this device.</FormHelperText>
              )}
            </FormControl>
          )}

          {hasFilters && (
            <Button size="small" onClick={clearFilters} sx={{ alignSelf: 'center' }}>
              Clear filters
            </Button>
          )}
        </Stack>

        <Box sx={{ mt: 2, display: 'flex', alignItems: 'center', gap: 2, flexWrap: 'wrap' }}>
          {canWrite && (
            <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>
              Add Rule
            </Button>
          )}
          {!canWrite && (
            <Chip size="small" label="View only" variant="outlined" />
          )}
          <Typography variant="caption" color="text.secondary">
            {filteredRules.length} rule{filteredRules.length === 1 ? '' : 's'}
            {hasFilters ? ' matching filters' : ' total'}
          </Typography>
        </Box>
      </Paper>

      {rulesError && (
        <MuiAlert severity="error" sx={{ mb: 3 }} action={
          <Button color="inherit" size="small" onClick={loadAllRules}>Retry</Button>
        }>
          {rulesError}
        </MuiAlert>
      )}

      {rulesLoading ? (
        <Paper sx={{ ...panelSx }}>
          <Stack spacing={1.5}>
            {[1, 2, 3].map((k) => <Skeleton key={k} variant="rounded" height={52} />)}
          </Stack>
        </Paper>
      ) : filteredRules.length === 0 && !rulesError ? (
        <Paper sx={{ ...panelSx }}>
          <Typography color="text.secondary">
            {allRules.length === 0
              ? 'No alert rules yet.'
              : 'No rules match these filters.'}
          </Typography>
          {canWrite && allRules.length === 0 && (
            <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.5 }}>
              Click <strong>Add Rule</strong> to create one.
            </Typography>
          )}
        </Paper>
      ) : filteredRules.length > 0 ? (
        <TableContainer component={Paper} sx={{ ...panelSx, p: 0 }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Rule Name</TableCell>
                <TableCell>Device</TableCell>
                <TableCell>Sensor</TableCell>
                <TableCell>Type</TableCell>
                <TableCell>Condition</TableCell>
                <TableCell>Severity</TableCell>
                <TableCell>Status</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {filteredRules.map((rule) => (
                <TableRow key={rule.alertRuleId} hover>
                  <TableCell>
                    <Typography variant="body2" fontWeight={500}>{rule.ruleName}</Typography>
                    <Typography variant="caption" color="text.secondary">{rule.condition}</Typography>
                  </TableCell>
                  <TableCell>
                    {rule.deviceId != null ? (
                      <Link to={`/devices/${rule.deviceId}`}>{deviceLabel(rule)}</Link>
                    ) : (
                      '—'
                    )}
                  </TableCell>
                  <TableCell>{sensorLabel(rule)}</TableCell>
                  <TableCell>
                    <Chip label={rule.ruleType} size="small" variant="outlined" />
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">{ruleConditionSummary(rule)}</Typography>
                  </TableCell>
                  <TableCell>
                    <Chip label={rule.severity} color={getSeverityColor(rule.severity)} size="small" />
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={rule.isEnabled ? 'Enabled' : 'Disabled'}
                      color={rule.isEnabled ? 'success' : 'default'}
                      size="small"
                      variant={rule.isEnabled ? 'filled' : 'outlined'}
                    />
                  </TableCell>
                  <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                    <IconButton size="small" onClick={() => openView(rule)} title="View">
                      <ViewIcon fontSize="small" />
                    </IconButton>
                    {canWrite && (
                      <IconButton size="small" onClick={() => openEdit(rule)} title="Edit">
                        <EditIcon fontSize="small" />
                      </IconButton>
                    )}
                    {canWrite && (
                      <IconButton size="small" onClick={() => handleDelete(rule)} title="Delete" color="error">
                        <DeleteIcon fontSize="small" />
                      </IconButton>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      ) : null}

      {/* View dialog */}
      <Dialog open={viewingRule != null} onClose={() => setViewingRule(null)} maxWidth="sm" fullWidth>
        <DialogTitle>Alert Rule Details</DialogTitle>
        <DialogContent>
          {viewingRule && (
            <Stack spacing={1.5} sx={{ mt: 1 }}>
              <Box>
                <Typography variant="caption" color="text.secondary">Rule Name</Typography>
                <Typography variant="body1">{viewingRule.ruleName}</Typography>
              </Box>
              <Box>
                <Typography variant="caption" color="text.secondary">Device / Sensor</Typography>
                <Typography variant="body2">
                  {deviceLabel(viewingRule)} · {sensorLabel(viewingRule)}
                </Typography>
              </Box>
              <Box>
                <Typography variant="caption" color="text.secondary">Type & Condition</Typography>
                <Typography variant="body2">
                  {viewingRule.ruleType} — {ruleConditionSummary(viewingRule)}
                </Typography>
              </Box>
              <Box>
                <Typography variant="caption" color="text.secondary">Alert Message</Typography>
                <Typography variant="body2">{viewingRule.condition}</Typography>
              </Box>
              <Stack direction="row" spacing={1}>
                <Chip label={viewingRule.severity} color={getSeverityColor(viewingRule.severity)} size="small" />
                <Chip
                  label={viewingRule.isEnabled ? 'Enabled' : 'Disabled'}
                  color={viewingRule.isEnabled ? 'success' : 'default'}
                  size="small"
                />
              </Stack>
              <Typography variant="caption" color="text.secondary">
                Created: {formatDateTime(viewingRule.createdAt)} · Updated: {formatDateTime(viewingRule.updatedAt)}
              </Typography>
            </Stack>
          )}
        </DialogContent>
        <DialogActions>
          {canWrite && viewingRule && (
            <Button
              onClick={() => {
                const rule = viewingRule;
                setViewingRule(null);
                openEdit(rule);
              }}
            >
              Edit
            </Button>
          )}
          <Button onClick={() => setViewingRule(null)}>Close</Button>
        </DialogActions>
      </Dialog>

      {/* Add / Edit Dialog */}
      <Dialog open={formOpen} onClose={closeForm} maxWidth="sm" fullWidth>
        <DialogTitle>
          {editingRule ? 'Edit Alert Rule' : 'Add Alert Rule'}
          {formContextSensor && (
            <Typography variant="caption" color="text.secondary" display="block">
              Sensor: {formContextSensor.sensorName} ({formContextSensor.sensorType}
              {formContextSensor.unit ? `, ${formContextSensor.unit}` : ''})
            </Typography>
          )}
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {formError && <MuiAlert severity="error">{formError}</MuiAlert>}

            {needsDialogDeviceSensor && (
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                <FormControl fullWidth required size="small">
                  <InputLabel>Device</InputLabel>
                  <Select
                    value={dialogDeviceId}
                    label="Device"
                    onChange={(e) => {
                      const v = e.target.value;
                      setDialogDeviceId(v === '' ? '' : (v as number));
                      setDialogSensorId('');
                    }}
                  >
                    {devices.map((d) => (
                      <MenuItem key={d.deviceId} value={d.deviceId}>{d.deviceName}</MenuItem>
                    ))}
                  </Select>
                </FormControl>
                <FormControl fullWidth required size="small" disabled={dialogDeviceId === '' || dialogSensorsLoading}>
                  <InputLabel>Sensor</InputLabel>
                  <Select
                    value={dialogSensorId}
                    label="Sensor"
                    onChange={(e) => {
                      const v = e.target.value;
                      setDialogSensorId(v === '' ? '' : (v as number));
                    }}
                  >
                    {dialogSensors.map((s) => (
                      <MenuItem key={s.sensorId} value={s.sensorId}>{s.sensorName}</MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Stack>
            )}

            <TextField
              label="Rule Name"
              value={form.ruleName}
              onChange={(e) => handleFormChange('ruleName', e.target.value)}
              required
              fullWidth
              placeholder="e.g. High Temperature Alert"
            />

            <FormControl fullWidth required>
              <InputLabel>Rule Type</InputLabel>
              <Select
                value={form.ruleType}
                label="Rule Type"
                onChange={(e) => handleFormChange('ruleType', e.target.value as RuleFormState['ruleType'])}
              >
                {RULE_TYPES.map((t) => (
                  <MenuItem key={t} value={t}>{t.charAt(0).toUpperCase() + t.slice(1)}</MenuItem>
                ))}
              </Select>
              <FormHelperText>
                {form.ruleType === 'threshold' && 'Fires when the value crosses a single threshold (e.g. > 35°C).'}
                {form.ruleType === 'range' && 'Fires when the value goes outside the min–max range (e.g. outside 18–28°C).'}
                {form.ruleType === 'change' && 'Fires whenever the sensor value changes from its previous reading.'}
              </FormHelperText>
            </FormControl>

            {form.ruleType === 'threshold' && (
              <Stack direction="row" spacing={2}>
                <FormControl sx={{ minWidth: 130 }} required>
                  <InputLabel>Operator</InputLabel>
                  <Select
                    value={form.comparisonOperator}
                    label="Operator"
                    onChange={(e) => handleFormChange('comparisonOperator', e.target.value)}
                  >
                    {OPERATORS.map((op) => <MenuItem key={op} value={op}>{op}</MenuItem>)}
                  </Select>
                </FormControl>
                <TextField
                  label="Threshold Value"
                  type="number"
                  value={form.thresholdValue}
                  onChange={(e) => handleFormChange('thresholdValue', e.target.value)}
                  required
                  fullWidth
                  helperText={formContextSensor?.unit ? `Unit: ${formContextSensor.unit}` : undefined}
                />
              </Stack>
            )}

            {form.ruleType === 'range' && (
              <Stack direction="row" spacing={2}>
                <TextField
                  label="Min Value"
                  type="number"
                  value={form.minValue}
                  onChange={(e) => handleFormChange('minValue', e.target.value)}
                  required
                  fullWidth
                  helperText={`Alert if below this${formContextSensor?.unit ? ` (${formContextSensor.unit})` : ''}`}
                />
                <TextField
                  label="Max Value"
                  type="number"
                  value={form.maxValue}
                  onChange={(e) => handleFormChange('maxValue', e.target.value)}
                  required
                  fullWidth
                  helperText={`Alert if above this${formContextSensor?.unit ? ` (${formContextSensor.unit})` : ''}`}
                />
              </Stack>
            )}

            <FormControl fullWidth required>
              <InputLabel>Severity</InputLabel>
              <Select
                value={form.severity}
                label="Severity"
                onChange={(e) => handleFormChange('severity', e.target.value)}
              >
                {SEVERITIES.map((s) => <MenuItem key={s} value={s}>{s}</MenuItem>)}
              </Select>
            </FormControl>

            <TextField
              label="Alert Message"
              value={form.condition}
              onChange={(e) => handleFormChange('condition', e.target.value)}
              required
              fullWidth
              helperText="This message is shown when the alert fires."
              multiline
              rows={2}
              placeholder={`e.g. Temperature exceeded safe limit on ${formContextSensor?.sensorName ?? 'sensor'}`}
            />

            <FormControlLabel
              control={<Switch checked={form.isEnabled} onChange={(e) => handleFormChange('isEnabled', e.target.checked)} />}
              label="Enabled"
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeForm} disabled={formSaving}>Cancel</Button>
          <Button onClick={handleSave} variant="contained" disabled={formSaving}>
            {formSaving ? 'Saving…' : editingRule ? 'Save Changes' : 'Add Rule'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default AlertRulesPage;
