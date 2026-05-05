import React, { useEffect, useState } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  Grid,
  Alert,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  FormControlLabel,
  Switch,
} from '@mui/material';
import { Actuator, CreateActuator, Sensor } from '../../types';

export type ActuatorFormValues = CreateActuator & { isActive?: boolean };

const emptyForm = (): ActuatorFormValues => ({
  name: '',
  description: '',
  kind: 'Discrete',
  channel: '',
  analogMin: undefined,
  analogMax: undefined,
  controlUnit: '',
  feedbackSensorId: undefined,
  isActive: true,
});

function actuatorToFormValues(a: Actuator): ActuatorFormValues {
  return {
    name: a.name,
    description: a.description ?? '',
    kind: a.kind,
    channel: a.channel ?? '',
    analogMin: a.analogMin,
    analogMax: a.analogMax,
    controlUnit: a.controlUnit ?? '',
    feedbackSensorId: a.feedbackSensorId,
    isActive: a.isActive,
  };
}

interface ActuatorFormProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (values: ActuatorFormValues) => Promise<void>;
  initialData?: Actuator;
  sensors: Sensor[];
  title?: string;
  isEdit?: boolean;
}

const ActuatorForm: React.FC<ActuatorFormProps> = ({
  open,
  onClose,
  onSubmit,
  initialData,
  sensors,
  title = 'Add Actuator',
  isEdit = false,
}) => {
  const [formData, setFormData] = useState<ActuatorFormValues>(() => emptyForm());
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;
    setFormData(initialData ? actuatorToFormValues(initialData) : emptyForm());
    setError(null);
  }, [open, initialData]);

  const handleSubmit = async () => {
    if (!formData.name.trim()) {
      setError('Name is required.');
      return;
    }
    if (
      formData.kind === 'Analog' &&
      formData.analogMin !== undefined &&
      formData.analogMax !== undefined &&
      formData.analogMin > formData.analogMax
    ) {
      setError('Analog min cannot be greater than max.');
      return;
    }
    try {
      setLoading(true);
      setError(null);
      await onSubmit(formData);
      onClose();
      setFormData(emptyForm());
    } catch (err: any) {
      console.error('Error submitting actuator form:', err);
      setError(err.message || 'Failed to save actuator.');
    } finally {
      setLoading(false);
    }
  };

  const isAnalog = formData.kind === 'Analog';

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}
        <Grid container spacing={2} sx={{ mt: 0.5 }}>
          <Grid item xs={12}>
            <TextField
              fullWidth
              label="Name"
              required
              value={formData.name}
              onChange={(e) => setFormData((f) => ({ ...f, name: e.target.value }))}
            />
          </Grid>
          <Grid item xs={12}>
            <TextField
              fullWidth
              label="Description"
              multiline
              minRows={2}
              value={formData.description ?? ''}
              onChange={(e) => setFormData((f) => ({ ...f, description: e.target.value }))}
            />
          </Grid>
          <Grid item xs={12} sm={6}>
            <FormControl fullWidth>
              <InputLabel id="act-form-kind">Kind</InputLabel>
              <Select
                labelId="act-form-kind"
                label="Kind"
                value={formData.kind}
                onChange={(e) => setFormData((f) => ({ ...f, kind: String(e.target.value) }))}
              >
                <MenuItem value="Discrete">Discrete (on/off)</MenuItem>
                <MenuItem value="Analog">Analog (numeric)</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Channel (firmware)"
              value={formData.channel ?? ''}
              onChange={(e) => setFormData((f) => ({ ...f, channel: e.target.value }))}
            />
          </Grid>
          {isAnalog && (
            <>
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Analog min"
                  type="number"
                  value={formData.analogMin ?? ''}
                  onChange={(e) =>
                    setFormData((f) => ({
                      ...f,
                      analogMin: e.target.value === '' ? undefined : Number(e.target.value),
                    }))
                  }
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Analog max"
                  type="number"
                  value={formData.analogMax ?? ''}
                  onChange={(e) =>
                    setFormData((f) => ({
                      ...f,
                      analogMax: e.target.value === '' ? undefined : Number(e.target.value),
                    }))
                  }
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  fullWidth
                  label="Control unit"
                  value={formData.controlUnit ?? ''}
                  onChange={(e) => setFormData((f) => ({ ...f, controlUnit: e.target.value }))}
                />
              </Grid>
            </>
          )}
          <Grid item xs={12}>
            <FormControl fullWidth>
              <InputLabel id="act-form-fb">Feedback sensor</InputLabel>
              <Select
                labelId="act-form-fb"
                label="Feedback sensor"
                value={formData.feedbackSensorId ?? ''}
                onChange={(e) => {
                  const v = e.target.value;
                  setFormData((f) => ({
                    ...f,
                    feedbackSensorId: v === '' ? undefined : Number(v),
                  }));
                }}
              >
                <MenuItem value="">None</MenuItem>
                {sensors.map((s) => (
                  <MenuItem key={s.sensorId} value={s.sensorId}>
                    {s.sensorName} ({s.sensorType}) — ID {s.sensorId}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>
          {isEdit && (
            <Grid item xs={12}>
              <FormControlLabel
                control={(
                  <Switch
                    checked={formData.isActive ?? true}
                    onChange={(_, v) => setFormData((f) => ({ ...f, isActive: v }))}
                  />
                )}
                label="Active"
              />
            </Grid>
          )}
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={() => void handleSubmit()} disabled={loading || !formData.name.trim()}>
          {loading ? 'Saving…' : 'Save'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default ActuatorForm;
