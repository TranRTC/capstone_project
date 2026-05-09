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
import { Add as AddIcon, Edit as EditIcon, DeleteOutline as DeleteIcon } from '@mui/icons-material';
import type { ChipProps } from '@mui/material/Chip';
import { apiService } from '../services/api';
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
      return `${rule.minValue ?? '—'} to ${rule.maxValue ?? '—'}`;
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
  sensorId: number | '';
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
  sensorId: '',
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
    sensorId: rule.sensorId ?? '',
  };
}

function formToPayload(form: RuleFormState, deviceId: number | ''): CreateAlertRule {
  const base: CreateAlertRule = {
    deviceId: deviceId !== '' ? deviceId : undefined,
    sensorId: form.sensorId !== '' ? form.sensorId : undefined,
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

const AlertRulesPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const [devices, setDevices] = useState<DeviceList[]>([]);
  const [devicesLoading, setDevicesLoading] = useState(true);
  const [devicesError, setDevicesError] = useState<string | null>(null);
  const [selectedDeviceId, setSelectedDeviceId] = useState<number | ''>('');
  const [sensors, setSensors] = useState<Sensor[]>([]);
  const [rules, setRules] = useState<AlertRule[]>([]);
  const [rulesLoading, setRulesLoading] = useState(false);
  const [rulesError, setRulesError] = useState<string | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [editingRule, setEditingRule] = useState<AlertRule | null>(null);
  const [form, setForm] = useState<RuleFormState>(emptyForm());
  const [formError, setFormError] = useState<string | null>(null);
  const [formSaving, setFormSaving] = useState(false);

  useEffect(() => {
    loadDevices();
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
      setRules([]);
      setSensors([]);
      setRulesError(null);
      return;
    }
    loadRules(selectedDeviceId);
    loadSensors(selectedDeviceId);
  }, [selectedDeviceId]);

  const loadDevices = async () => {
    try {
      setDevicesLoading(true);
      setDevicesError(null);
      const data = await apiService.getDevices();
      setDevices(data);
      if (data.length > 0) {
        const qRaw = searchParams.get('deviceId');
        const qId = qRaw != null ? Number.parseInt(qRaw, 10) : NaN;
        if (Number.isFinite(qId) && data.some((d) => d.deviceId === qId)) {
          setSelectedDeviceId(qId);
        } else {
          setSelectedDeviceId((prev) => (prev === '' ? data[0].deviceId : prev));
        }
      }
    } catch (err: any) {
      setDevicesError(err?.message || 'Failed to load devices');
    } finally {
      setDevicesLoading(false);
    }
  };

  const loadRules = async (deviceId: number) => {
    try {
      setRulesLoading(true);
      setRulesError(null);
      const data = await apiService.getAlertRulesByDevice(deviceId);
      setRules(data);
    } catch (err: any) {
      setRulesError(err?.message || 'Failed to load alert rules');
      setRules([]);
    } finally {
      setRulesLoading(false);
    }
  };

  const loadSensors = async (deviceId: number) => {
    try {
      const data = await apiService.getSensorsByDevice(deviceId);
      setSensors(data);
    } catch {
      setSensors([]);
    }
  };

  const openCreate = () => {
    setEditingRule(null);
    setForm(emptyForm());
    setFormError(null);
    setFormOpen(true);
  };

  const openEdit = (rule: AlertRule) => {
    setEditingRule(rule);
    setForm(ruleToForm(rule));
    setFormError(null);
    setFormOpen(true);
  };

  const closeForm = () => {
    setFormOpen(false);
    setEditingRule(null);
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

    setFormSaving(true);
    setFormError(null);
    try {
      const payload = formToPayload(form, selectedDeviceId);
      if (editingRule) {
        await apiService.updateAlertRule(editingRule.alertRuleId, payload);
      } else {
        await apiService.createAlertRule(payload);
      }
      closeForm();
      if (selectedDeviceId !== '') await loadRules(selectedDeviceId);
    } catch (err: any) {
      setFormError(err?.message || 'Failed to save rule.');
    } finally {
      setFormSaving(false);
    }
  };

  const handleDelete = async (rule: AlertRule) => {
    if (!window.confirm(`Delete rule "${rule.ruleName}"? This cannot be undone.`)) return;
    try {
      await apiService.deleteAlertRule(rule.alertRuleId);
      if (selectedDeviceId !== '') await loadRules(selectedDeviceId);
    } catch (err: any) {
      alert(err?.message || 'Failed to delete rule.');
    }
  };

  const canManage = selectedDeviceId !== '' && !devicesLoading && devices.length > 0;

  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom sx={{ mb: 2 }}>
        Alert Rules
      </Typography>

      {devicesError && (
        <MuiAlert severity="error" sx={{ mb: 3 }} action={<Button color="inherit" size="small" onClick={loadDevices}>Retry</Button>}>
          {devicesError}
        </MuiAlert>
      )}

      <Paper sx={{ ...panelSx, mb: 3 }}>
        <Typography variant="subtitle2" color="text.secondary" sx={{ mb: 1.5 }}>
          Select a device to view and manage its alert rules.
        </Typography>
        {devicesLoading ? (
          <Skeleton variant="rounded" width="100%" height={56} sx={{ maxWidth: 400 }} />
        ) : (
          <FormControl fullWidth sx={{ maxWidth: 400 }} disabled={devices.length === 0}>
            <InputLabel id="ar-device-select-label">Device</InputLabel>
            <Select
              labelId="ar-device-select-label"
              value={selectedDeviceId}
              label="Device"
              onChange={(e) => setSelectedDeviceId(e.target.value as number)}
            >
              {devices.map((d) => (
                <MenuItem key={d.deviceId} value={d.deviceId}>
                  {d.deviceName} ({d.deviceType})
                </MenuItem>
              ))}
            </Select>
            {devices.length === 0 ? (
              <FormHelperText>No devices yet. <Link to="/devices">Register a device</Link> first.</FormHelperText>
            ) : (
              <FormHelperText>
                {selectedDeviceId !== '' && (
                  <><Link to={`/devices/${selectedDeviceId}`}>Open device detail</Link></>
                )}
              </FormHelperText>
            )}
          </FormControl>
        )}
        <Box sx={{ mt: 2 }}>
          <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate} disabled={!canManage || rulesLoading}>
            Add Rule
          </Button>
        </Box>
      </Paper>

      {rulesError && selectedDeviceId !== '' && (
        <MuiAlert severity="error" sx={{ mb: 3 }} action={<Button color="inherit" size="small" onClick={() => loadRules(selectedDeviceId as number)}>Retry</Button>}>
          {rulesError}
        </MuiAlert>
      )}

      {selectedDeviceId !== '' && rulesLoading ? (
        <Paper sx={{ ...panelSx }}>
          <Stack spacing={1.5}>
            {[1, 2, 3].map((k) => <Skeleton key={k} variant="rounded" height={52} />)}
          </Stack>
        </Paper>
      ) : selectedDeviceId !== '' && rules.length === 0 && !rulesError ? (
        <Paper sx={{ ...panelSx }}>
          <Typography color="text.secondary">No alert rules for this device yet.</Typography>
          <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.5 }}>
            Use Add Rule to create one.
          </Typography>
        </Paper>
      ) : rules.length > 0 ? (
        <TableContainer component={Paper} sx={{ ...panelSx, p: 0 }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Rule Name</TableCell>
                <TableCell>Type</TableCell>
                <TableCell>Condition</TableCell>
                <TableCell>Sensor</TableCell>
                <TableCell>Severity</TableCell>
                <TableCell>Status</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {rules.map((rule) => {
                const sensor = sensors.find((s) => s.sensorId === rule.sensorId);
                return (
                  <TableRow key={rule.alertRuleId} hover>
                    <TableCell>
                      <Typography variant="body2" fontWeight={500}>{rule.ruleName}</Typography>
                      <Typography variant="caption" color="text.secondary">{rule.condition}</Typography>
                    </TableCell>
                    <TableCell>
                      <Chip label={rule.ruleType} size="small" variant="outlined" />
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">{ruleConditionSummary(rule)}</Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">{sensor ? sensor.sensorName : rule.sensorId ? `Sensor ${rule.sensorId}` : 'All sensors'}</Typography>
                    </TableCell>
                    <TableCell>
                      <Chip label={rule.severity} color={getSeverityColor(rule.severity)} size="small" />
                    </TableCell>
                    <TableCell>
                      <Chip label={rule.isEnabled ? 'Enabled' : 'Disabled'} color={rule.isEnabled ? 'success' : 'default'} size="small" variant={rule.isEnabled ? 'filled' : 'outlined'} />
                    </TableCell>
                    <TableCell align="right">
                      <IconButton size="small" onClick={() => openEdit(rule)} title="Edit">
                        <EditIcon fontSize="small" />
                      </IconButton>
                      <IconButton size="small" onClick={() => handleDelete(rule)} title="Delete" color="error">
                        <DeleteIcon fontSize="small" />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </TableContainer>
      ) : null}

      {/* Add / Edit Dialog */}
      <Dialog open={formOpen} onClose={closeForm} maxWidth="sm" fullWidth>
        <DialogTitle>{editingRule ? 'Edit Alert Rule' : 'Add Alert Rule'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {formError && <MuiAlert severity="error">{formError}</MuiAlert>}

            <TextField
              label="Rule Name"
              value={form.ruleName}
              onChange={(e) => handleFormChange('ruleName', e.target.value)}
              required
              fullWidth
            />

            <FormControl fullWidth required>
              <InputLabel>Rule Type</InputLabel>
              <Select
                value={form.ruleType}
                label="Rule Type"
                onChange={(e) => handleFormChange('ruleType', e.target.value as RuleFormState['ruleType'])}
              >
                {RULE_TYPES.map((t) => <MenuItem key={t} value={t}>{t.charAt(0).toUpperCase() + t.slice(1)}</MenuItem>)}
              </Select>
              <FormHelperText>
                {form.ruleType === 'threshold' && 'Fires when value crosses a single threshold.'}
                {form.ruleType === 'range' && 'Fires when value goes outside the min–max range.'}
                {form.ruleType === 'change' && 'Fires when the sensor value changes from its previous reading.'}
              </FormHelperText>
            </FormControl>

            {/* Threshold fields */}
            {form.ruleType === 'threshold' && (
              <Stack direction="row" spacing={2}>
                <FormControl sx={{ minWidth: 120 }} required>
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
                />
              </Stack>
            )}

            {/* Range fields */}
            {form.ruleType === 'range' && (
              <Stack direction="row" spacing={2}>
                <TextField
                  label="Min Value"
                  type="number"
                  value={form.minValue}
                  onChange={(e) => handleFormChange('minValue', e.target.value)}
                  required
                  fullWidth
                  helperText="Alert if value goes below this"
                />
                <TextField
                  label="Max Value"
                  type="number"
                  value={form.maxValue}
                  onChange={(e) => handleFormChange('maxValue', e.target.value)}
                  required
                  fullWidth
                  helperText="Alert if value goes above this"
                />
              </Stack>
            )}

            <FormControl fullWidth>
              <InputLabel>Sensor (optional)</InputLabel>
              <Select
                value={form.sensorId}
                label="Sensor (optional)"
                onChange={(e) => handleFormChange('sensorId', e.target.value as number | '')}
              >
                <MenuItem value="">All sensors on this device</MenuItem>
                {sensors.map((s) => (
                  <MenuItem key={s.sensorId} value={s.sensorId}>
                    {s.sensorName} ({s.sensorType}{s.unit ? `, ${s.unit}` : ''})
                  </MenuItem>
                ))}
              </Select>
              <FormHelperText>Leave blank to apply to all sensors on this device.</FormHelperText>
            </FormControl>

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
              label="Condition / Alert Message"
              value={form.condition}
              onChange={(e) => handleFormChange('condition', e.target.value)}
              required
              fullWidth
              helperText="This message appears on the alert when it fires."
              multiline
              rows={2}
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
