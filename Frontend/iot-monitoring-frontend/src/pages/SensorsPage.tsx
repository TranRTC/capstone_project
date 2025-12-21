import React, { useEffect, useState } from 'react';
import {
  Box,
  Typography,
  Paper,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Grid,
  Card,
  CardContent,
} from '@mui/material';
import { apiService } from '../services/api';
import { DeviceList, Sensor } from '../types';

const SensorsPage: React.FC = () => {
  const [devices, setDevices] = useState<DeviceList[]>([]);
  const [selectedDeviceId, setSelectedDeviceId] = useState<number | ''>('');
  const [sensors, setSensors] = useState<Sensor[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadDevices();
  }, []);

  useEffect(() => {
    if (selectedDeviceId) {
      loadSensors(selectedDeviceId);
    } else {
      setSensors([]);
    }
  }, [selectedDeviceId]);

  const loadDevices = async () => {
    try {
      const data = await apiService.getDevices();
      setDevices(data);
      if (data.length > 0 && !selectedDeviceId) {
        setSelectedDeviceId(data[0].deviceId);
      }
    } catch (error) {
      console.error('Error loading devices:', error);
    }
  };

  const loadSensors = async (deviceId: number) => {
    try {
      setLoading(true);
      const data = await apiService.getSensorsByDevice(deviceId);
      setSensors(data);
    } catch (error) {
      console.error('Error loading sensors:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom>
        Sensors
      </Typography>

      <FormControl fullWidth sx={{ mb: 3, maxWidth: 400 }}>
        <InputLabel>Select Device</InputLabel>
        <Select
          value={selectedDeviceId}
          label="Select Device"
          onChange={(e) => setSelectedDeviceId(e.target.value as number)}
        >
          {devices.map((device) => (
            <MenuItem key={device.deviceId} value={device.deviceId}>
              {device.deviceName} ({device.deviceType})
            </MenuItem>
          ))}
        </Select>
      </FormControl>

      {loading ? (
        <Typography>Loading sensors...</Typography>
      ) : sensors.length === 0 ? (
        <Paper sx={{ p: 3 }}>
          <Typography>
            {selectedDeviceId
              ? 'No sensors found for this device'
              : 'Please select a device to view sensors'}
          </Typography>
        </Paper>
      ) : (
        <Grid container spacing={3}>
          {sensors.map((sensor) => (
            <Grid item xs={12} sm={6} md={4} key={sensor.sensorId}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    {sensor.sensorName}
                  </Typography>
                  <Typography variant="body2" color="textSecondary" gutterBottom>
                    Type: {sensor.sensorType}
                  </Typography>
                  {sensor.unit && (
                    <Typography variant="body2" color="textSecondary">
                      Unit: {sensor.unit}
                    </Typography>
                  )}
                  {sensor.minValue !== null && sensor.maxValue !== null && (
                    <Typography variant="body2" color="textSecondary">
                      Range: {sensor.minValue} - {sensor.maxValue} {sensor.unit}
                    </Typography>
                  )}
                  <Typography
                    variant="caption"
                    color={sensor.isActive ? 'success.main' : 'text.secondary'}
                    sx={{ display: 'block', mt: 1 }}
                  >
                    {sensor.isActive ? 'Active' : 'Inactive'}
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}
    </Box>
  );
};

export default SensorsPage;
