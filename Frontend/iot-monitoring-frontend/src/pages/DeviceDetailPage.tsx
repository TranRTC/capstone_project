import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Box,
  Typography,
  Paper,
  Grid,
  Card,
  CardContent,
  Button,
  Chip,
  CircularProgress,
  Breadcrumbs,
  Link,
} from '@mui/material';
import { ArrowBack as ArrowBackIcon } from '@mui/icons-material';
import { apiService } from '../services/api';
import DeviceTemperatureChart from '../components/charts/DeviceTemperatureChart';
import { Device } from '../types';

const DeviceDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [device, setDevice] = useState<Device | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (id) {
      loadDevice(parseInt(id));
    }
  }, [id]);

  const loadDevice = async (deviceId: number) => {
    try {
      setLoading(true);
      setError(null);
      const deviceData = await apiService.getDevice(deviceId);
      setDevice(deviceData);
    } catch (err: any) {
      console.error('Error loading device:', err);
      setError(err.message || 'Failed to load device');
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '400px' }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error || !device) {
    return (
      <Box>
        <Button
          startIcon={<ArrowBackIcon />}
          onClick={() => navigate('/devices')}
          sx={{ mb: 2 }}
        >
          Back to Devices
        </Button>
        <Paper sx={{ p: 3 }}>
          <Typography color="error" variant="h6">
            {error || 'Device not found'}
          </Typography>
        </Paper>
      </Box>
    );
  }

  return (
    <Box>
      {/* Breadcrumbs */}
      <Breadcrumbs sx={{ mb: 2 }}>
        <Link
          component="button"
          variant="body1"
          onClick={() => navigate('/devices')}
          sx={{ cursor: 'pointer', textDecoration: 'none' }}
        >
          Devices
        </Link>
        <Typography color="text.primary">{device.deviceName}</Typography>
      </Breadcrumbs>

      {/* Back Button */}
      <Button
        startIcon={<ArrowBackIcon />}
        onClick={() => navigate('/devices')}
        sx={{ mb: 3 }}
      >
        Back to Devices
      </Button>

      {/* Device Information */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12}>
          <Paper sx={{ p: 3 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
              <Typography variant="h4" component="h1">
                {device.deviceName}
              </Typography>
              <Chip
                label={device.isActive ? 'Active' : 'Inactive'}
                color={device.isActive ? 'success' : 'default'}
                size="medium"
              />
            </Box>

            <Grid container spacing={3}>
              <Grid item xs={12} md={6}>
                <Card variant="outlined">
                  <CardContent>
                    <Typography color="textSecondary" gutterBottom>
                      Device ID
                    </Typography>
                    <Typography variant="h6">{device.deviceId}</Typography>
                  </CardContent>
                </Card>
              </Grid>

              <Grid item xs={12} md={6}>
                <Card variant="outlined">
                  <CardContent>
                    <Typography color="textSecondary" gutterBottom>
                      Device Type
                    </Typography>
                    <Typography variant="h6">{device.deviceType}</Typography>
                  </CardContent>
                </Card>
              </Grid>

              {device.location && (
                <Grid item xs={12} md={6}>
                  <Card variant="outlined">
                    <CardContent>
                      <Typography color="textSecondary" gutterBottom>
                        Location
                      </Typography>
                      <Typography variant="h6">{device.location}</Typography>
                    </CardContent>
                  </Card>
                </Grid>
              )}

              {device.facilityType && (
                <Grid item xs={12} md={6}>
                  <Card variant="outlined">
                    <CardContent>
                      <Typography color="textSecondary" gutterBottom>
                        Facility Type
                      </Typography>
                      <Typography variant="h6">{device.facilityType}</Typography>
                    </CardContent>
                  </Card>
                </Grid>
              )}

              {device.edgeDeviceType && (
                <Grid item xs={12} md={6}>
                  <Card variant="outlined">
                    <CardContent>
                      <Typography color="textSecondary" gutterBottom>
                        Edge Device Type
                      </Typography>
                      <Typography variant="h6">{device.edgeDeviceType}</Typography>
                    </CardContent>
                  </Card>
                </Grid>
              )}

              {device.edgeDeviceId && (
                <Grid item xs={12} md={6}>
                  <Card variant="outlined">
                    <CardContent>
                      <Typography color="textSecondary" gutterBottom>
                        Edge Device ID
                      </Typography>
                      <Typography variant="h6">{device.edgeDeviceId}</Typography>
                    </CardContent>
                  </Card>
                </Grid>
              )}

              <Grid item xs={12} md={6}>
                <Card variant="outlined">
                  <CardContent>
                    <Typography color="textSecondary" gutterBottom>
                      Last Seen
                    </Typography>
                    <Typography variant="h6">
                      {device.lastSeenAt
                        ? new Date(device.lastSeenAt).toLocaleString()
                        : 'Never'}
                    </Typography>
                  </CardContent>
                </Card>
              </Grid>

              <Grid item xs={12} md={6}>
                <Card variant="outlined">
                  <CardContent>
                    <Typography color="textSecondary" gutterBottom>
                      Created At
                    </Typography>
                    <Typography variant="h6">
                      {new Date(device.createdAt).toLocaleString()}
                    </Typography>
                  </CardContent>
                </Card>
              </Grid>

              {device.description && (
                <Grid item xs={12}>
                  <Card variant="outlined">
                    <CardContent>
                      <Typography color="textSecondary" gutterBottom>
                        Description
                      </Typography>
                      <Typography variant="body1">{device.description}</Typography>
                    </CardContent>
                  </Card>
                </Grid>
              )}
            </Grid>
          </Paper>
        </Grid>
      </Grid>

      {/* Temperature Chart - Larger and more prominent */}
      <Paper sx={{ p: 3 }}>
        <Typography variant="h5" component="h2" gutterBottom>
          Real-Time Temperature Monitoring
        </Typography>
        <Box sx={{ mt: 2 }}>
          <DeviceTemperatureChart
            deviceId={device.deviceId}
            deviceName={device.deviceName}
            height={500}
            showPaper={false}
          />
        </Box>
      </Paper>
    </Box>
  );
};

export default DeviceDetailPage;

