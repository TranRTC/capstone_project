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
  FormControlLabel,
  Switch,
} from '@mui/material';
import { CreateSensor } from '../../types';

export type SensorFormValues = CreateSensor & { isActive?: boolean };

const emptySensorForm = (): SensorFormValues => ({
  sensorName: '',
  sensorType: '',
  unit: '',
  edgeDeviceId: '',
  minValue: undefined,
  maxValue: undefined,
  isActive: true,
});

interface SensorFormProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (values: SensorFormValues) => Promise<void>;
  initialData?: SensorFormValues;
  title?: string;
  isEdit?: boolean;
}

const SensorForm: React.FC<SensorFormProps> = ({
  open,
  onClose,
  onSubmit,
  initialData,
  title = 'Add Sensor',
  isEdit = false,
}) => {
  const [formData, setFormData] = useState<SensorFormValues>(() => emptySensorForm());
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;
    setFormData({
      sensorName: initialData?.sensorName ?? '',
      sensorType: initialData?.sensorType ?? '',
      unit: initialData?.unit ?? '',
      edgeDeviceId: initialData?.edgeDeviceId ?? '',
      minValue: initialData?.minValue,
      maxValue: initialData?.maxValue,
      isActive: initialData?.isActive ?? true,
    });
    setError(null);
  }, [open, initialData]);

  const handleChange =
    (field: keyof SensorFormValues) => (e: React.ChangeEvent<HTMLInputElement>) => {
      setFormData({ ...formData, [field]: e.target.value });
    };

  const handleNumberChange =
    (field: 'minValue' | 'maxValue') => (e: React.ChangeEvent<HTMLInputElement>) => {
      const v = e.target.value;
      setFormData({
        ...formData,
        [field]: v === '' ? undefined : Number(v),
      });
    };

  const handleSubmit = async () => {
    if (
      formData.minValue !== undefined &&
      formData.maxValue !== undefined &&
      formData.minValue > formData.maxValue
    ) {
      setError('Min value cannot be greater than max value.');
      return;
    }
    try {
      setLoading(true);
      setError(null);
      await onSubmit(formData);
      onClose();
      setFormData(emptySensorForm());
    } catch (err: any) {
      console.error('Error submitting sensor form:', err);
      setError(err.message || 'Failed to save sensor. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const canSave =
    formData.sensorName.trim().length > 0 && formData.sensorType.trim().length > 0;

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}
        <Grid container spacing={2} sx={{ mt: 1 }}>
          <Grid item xs={12}>
            <TextField
              fullWidth
              label="Sensor Name"
              required
              value={formData.sensorName}
              onChange={handleChange('sensorName')}
            />
          </Grid>
          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Sensor Type"
              required
              placeholder="e.g. Temperature, Pressure"
              helperText="Max 50 characters"
              value={formData.sensorType}
              onChange={handleChange('sensorType')}
              inputProps={{ maxLength: 50 }}
            />
          </Grid>
          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Unit"
              placeholder="e.g. °C, kPa"
              helperText="Max 20 characters"
              value={formData.unit ?? ''}
              onChange={handleChange('unit')}
              inputProps={{ maxLength: 20 }}
            />
          </Grid>
          <Grid item xs={12}>
            <TextField
              fullWidth
              label="Edge Device ID"
              helperText="Optional — ties this sensor to an edge device identifier"
              value={formData.edgeDeviceId ?? ''}
              onChange={handleChange('edgeDeviceId')}
              inputProps={{ maxLength: 100 }}
            />
          </Grid>
          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Min value"
              type="number"
              value={formData.minValue === undefined ? '' : formData.minValue}
              onChange={handleNumberChange('minValue')}
              inputProps={{ step: 'any' }}
            />
          </Grid>
          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Max value"
              type="number"
              value={formData.maxValue === undefined ? '' : formData.maxValue}
              onChange={handleNumberChange('maxValue')}
              inputProps={{ step: 'any' }}
            />
          </Grid>
          {isEdit && (
            <Grid item xs={12}>
              <FormControlLabel
                control={(
                  <Switch
                    checked={formData.isActive ?? true}
                    onChange={(e) =>
                      setFormData({ ...formData, isActive: e.target.checked })
                    }
                  />
                )}
                label="Active"
              />
            </Grid>
          )}
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={loading}>
          Cancel
        </Button>
        <Button onClick={handleSubmit} variant="contained" disabled={loading || !canSave}>
          {loading ? 'Saving...' : 'Save'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default SensorForm;
