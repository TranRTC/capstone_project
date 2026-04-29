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
import { CreateDevice, DeviceCapabilitiesInput } from '../../types';

const emptyDeviceForm = (): CreateDevice => ({
  deviceName: '',
  deviceType: '',
  location: '',
  facilityType: '',
  edgeDeviceType: '',
  edgeDeviceId: '',
  description: '',
});

interface DeviceFormProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (device: CreateDevice, capabilities: DeviceCapabilitiesInput) => Promise<void>;
  initialData?: CreateDevice;
  initialCapabilities?: DeviceCapabilitiesInput;
  title?: string;
}

const defaultCapabilities = (deviceType?: string): DeviceCapabilitiesInput => {
  const lowered = (deviceType ?? '').toLowerCase();
  const actuatorLike =
    lowered.includes('motor') || lowered.includes('actuator') || lowered.includes('controller');

  return {
    supportsTelemetry: true,
    supportsPowerControl: actuatorLike,
    supportsAnalogControl: actuatorLike,
    analogMin: 0,
    analogMax: 100,
    analogStep: 1,
    controlUnit: '',
  };
};

const DeviceForm: React.FC<DeviceFormProps> = ({
  open,
  onClose,
  onSubmit,
  initialData,
  initialCapabilities,
  title = 'Create Device',
}) => {
  const [formData, setFormData] = useState<CreateDevice>(() => emptyDeviceForm());
  const [capabilities, setCapabilities] = useState<DeviceCapabilitiesInput>(() => defaultCapabilities());

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;
    setFormData({
      deviceName: initialData?.deviceName ?? '',
      deviceType: initialData?.deviceType ?? '',
      location: initialData?.location ?? '',
      facilityType: initialData?.facilityType ?? '',
      edgeDeviceType: initialData?.edgeDeviceType ?? '',
      edgeDeviceId: initialData?.edgeDeviceId ?? '',
      description: initialData?.description ?? '',
    });
    setCapabilities(
      initialCapabilities ?? defaultCapabilities(initialData?.deviceType)
    );
    setError(null);
  }, [open, initialData, initialCapabilities]);

  const handleChange = (field: keyof CreateDevice) => (
    e: React.ChangeEvent<HTMLInputElement>
  ) => {
    setFormData({ ...formData, [field]: e.target.value });
  };

  const handleSubmit = async () => {
    try {
      setLoading(true);
      setError(null);
      await onSubmit(formData, capabilities);
      onClose();
      setFormData(emptyDeviceForm());
      setCapabilities(defaultCapabilities());
    } catch (error: any) {
      console.error('Error submitting form:', error);
      setError(error.message || 'Failed to save device. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
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
              label="Device Name"
              required
              value={formData.deviceName}
              onChange={handleChange('deviceName')}
            />
          </Grid>
          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Device Type"
              required
              placeholder="e.g. Motor, Room, Pump"
              helperText="Equipment or asset category (max 50 characters)"
              value={formData.deviceType}
              onChange={handleChange('deviceType')}
              onBlur={() => {
                if (!initialCapabilities) {
                  setCapabilities((prev) => {
                    const defaults = defaultCapabilities(formData.deviceType);
                    return {
                      ...prev,
                      supportsTelemetry: defaults.supportsTelemetry,
                      supportsPowerControl: defaults.supportsPowerControl,
                      supportsAnalogControl: defaults.supportsAnalogControl,
                    };
                  });
                }
              }}
              inputProps={{ maxLength: 50 }}
            />
          </Grid>
          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Location"
              value={formData.location}
              onChange={handleChange('location')}
            />
          </Grid>
          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Facility Type"
              value={formData.facilityType}
              onChange={handleChange('facilityType')}
            />
          </Grid>
          <Grid item xs={12} sm={6}>
            <TextField
              fullWidth
              label="Edge Device Type"
              value={formData.edgeDeviceType}
              onChange={handleChange('edgeDeviceType')}
            />
          </Grid>
          <Grid item xs={12}>
            <TextField
              fullWidth
              label="Edge Device ID"
              value={formData.edgeDeviceId}
              onChange={handleChange('edgeDeviceId')}
            />
          </Grid>
          <Grid item xs={12}>
            <TextField
              fullWidth
              label="Description"
              multiline
              rows={3}
              value={formData.description}
              onChange={handleChange('description')}
            />
          </Grid>
          <Grid item xs={12}>
            <Alert severity="info">
              Device capabilities drive the Device Detail UI and command validation.
            </Alert>
          </Grid>
          <Grid item xs={12} sm={4}>
            <FormControlLabel
              control={(
                <Switch
                  checked={capabilities.supportsTelemetry}
                  onChange={(_, checked) =>
                    setCapabilities((prev) => ({ ...prev, supportsTelemetry: checked }))
                  }
                />
              )}
              label="Supports Telemetry"
            />
          </Grid>
          <Grid item xs={12} sm={4}>
            <FormControlLabel
              control={(
                <Switch
                  checked={capabilities.supportsPowerControl}
                  onChange={(_, checked) =>
                    setCapabilities((prev) => ({ ...prev, supportsPowerControl: checked }))
                  }
                />
              )}
              label="Supports Power Control"
            />
          </Grid>
          <Grid item xs={12} sm={4}>
            <FormControlLabel
              control={(
                <Switch
                  checked={capabilities.supportsAnalogControl}
                  onChange={(_, checked) =>
                    setCapabilities((prev) => ({ ...prev, supportsAnalogControl: checked }))
                  }
                />
              )}
              label="Supports Analog Control"
            />
          </Grid>
          {capabilities.supportsAnalogControl && (
            <>
              <Grid item xs={12} sm={3}>
                <TextField
                  fullWidth
                  label="Analog Min"
                  type="number"
                  value={capabilities.analogMin ?? 0}
                  onChange={(e) =>
                    setCapabilities((prev) => ({ ...prev, analogMin: Number(e.target.value) }))
                  }
                />
              </Grid>
              <Grid item xs={12} sm={3}>
                <TextField
                  fullWidth
                  label="Analog Max"
                  type="number"
                  value={capabilities.analogMax ?? 100}
                  onChange={(e) =>
                    setCapabilities((prev) => ({ ...prev, analogMax: Number(e.target.value) }))
                  }
                />
              </Grid>
              <Grid item xs={12} sm={3}>
                <TextField
                  fullWidth
                  label="Analog Step"
                  type="number"
                  value={capabilities.analogStep ?? 1}
                  onChange={(e) =>
                    setCapabilities((prev) => ({ ...prev, analogStep: Number(e.target.value) }))
                  }
                />
              </Grid>
              <Grid item xs={12} sm={3}>
                <TextField
                  fullWidth
                  label="Control Unit"
                  value={capabilities.controlUnit ?? ''}
                  onChange={(e) =>
                    setCapabilities((prev) => ({ ...prev, controlUnit: e.target.value }))
                  }
                />
              </Grid>
            </>
          )}
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={loading}>
          Cancel
        </Button>
        <Button
          onClick={handleSubmit}
          variant="contained"
          disabled={loading || !formData.deviceName.trim() || !formData.deviceType.trim()}
        >
          {loading ? 'Saving...' : 'Save'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default DeviceForm;

