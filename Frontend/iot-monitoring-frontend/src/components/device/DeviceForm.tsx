import React, { useState } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  Grid,
  MenuItem,
  Alert,
} from '@mui/material';
import { CreateDevice } from '../../types';

interface DeviceFormProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (device: CreateDevice) => Promise<void>;
  initialData?: CreateDevice;
  title?: string;
}

const DeviceForm: React.FC<DeviceFormProps> = ({
  open,
  onClose,
  onSubmit,
  initialData,
  title = 'Create Device',
}) => {
  const [formData, setFormData] = useState<CreateDevice>({
    deviceName: initialData?.deviceName || '',
    deviceType: initialData?.deviceType || '',
    location: initialData?.location || '',
    facilityType: initialData?.facilityType || '',
    edgeDeviceType: initialData?.edgeDeviceType || '',
    edgeDeviceId: initialData?.edgeDeviceId || '',
    description: initialData?.description || '',
  });

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleChange = (field: keyof CreateDevice) => (
    e: React.ChangeEvent<HTMLInputElement>
  ) => {
    setFormData({ ...formData, [field]: e.target.value });
  };

  const handleSubmit = async () => {
    try {
      setLoading(true);
      setError(null);
      await onSubmit(formData);
      onClose();
      setFormData({
        deviceName: '',
        deviceType: '',
        location: '',
        facilityType: '',
        edgeDeviceType: '',
        edgeDeviceId: '',
        description: '',
      });
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
              select
              value={formData.deviceType}
              onChange={handleChange('deviceType')}
            >
              <MenuItem value="Temperature">Temperature</MenuItem>
              <MenuItem value="Humidity">Humidity</MenuItem>
              <MenuItem value="Pressure">Pressure</MenuItem>
              <MenuItem value="Motion">Motion</MenuItem>
              <MenuItem value="Other">Other</MenuItem>
            </TextField>
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
        </Grid>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={loading}>
          Cancel
        </Button>
        <Button onClick={handleSubmit} variant="contained" disabled={loading || !formData.deviceName || !formData.deviceType}>
          {loading ? 'Saving...' : 'Save'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default DeviceForm;

