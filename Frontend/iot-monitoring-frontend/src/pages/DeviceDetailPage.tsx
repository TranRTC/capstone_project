import React, { useEffect, useState } from 'react';
import { useParams, useNavigate, Link as RouterLink } from 'react-router-dom';
import {
  Box,
  Typography,
  Paper,
  Grid,
  Card,
  CardContent,
  Button,
  Chip,
  Skeleton,
  Breadcrumbs,
  Link,
  Alert as MuiAlert,
} from '@mui/material';
import { apiService } from '../services/api';
import { Device } from '../types';

const panelSx = {
  p: 2.5,
  border: 1,
  borderColor: 'divider',
  boxShadow: 'none',
  bgcolor: 'background.paper',
};

const DeviceDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [device, setDevice] = useState<Device | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) {
      setDevice(null);
      setError('Invalid device link.');
      setLoading(false);
      return;
    }
    const deviceId = Number.parseInt(id, 10);
    if (!Number.isFinite(deviceId)) {
      setDevice(null);
      setError('Invalid device ID.');
      setLoading(false);
      return;
    }
    loadDevice(deviceId);
  }, [id]);

  const loadDevice = async (deviceId: number) => {
    try {
      setLoading(true);
      setError(null);
      const deviceData = await apiService.getDevice(deviceId);
      setDevice(deviceData);
    } catch (err: any) {
      console.error('Error loading device:', err);
      setDevice(null);
      setError(err.message || 'Failed to load device');
    } finally {
      setLoading(false);
    }
  };

  const parsedId = id ? Number.parseInt(id, 10) : NaN;
  const canRetry = Number.isFinite(parsedId);

  if (loading) {
    return (
      <Box>
        <Skeleton variant="text" width={280} height={32} sx={{ mb: 2 }} />
        <Paper sx={{ ...panelSx, mb: 3 }}>
          <Skeleton variant="text" width="60%" height={48} sx={{ mb: 2 }} />
          <Grid container spacing={2}>
            {[1, 2, 3, 4].map((k) => (
              <Grid item xs={12} sm={6} md={3} key={k}>
                <Skeleton variant="rounded" height={88} />
              </Grid>
            ))}
          </Grid>
        </Paper>
      </Box>
    );
  }

  if (error || !device) {
    return (
      <Box>
        <Breadcrumbs sx={{ mb: 2 }}>
          <Link component={RouterLink} to="/devices" color="inherit" underline="hover">
            Devices
          </Link>
          <Typography color="text.primary">Detail</Typography>
        </Breadcrumbs>

        <MuiAlert
          severity="error"
          sx={{ mb: 2 }}
          action={(
            <Button
              color="inherit"
              size="small"
              disabled={!canRetry}
              onClick={() => canRetry && loadDevice(parsedId)}
            >
              Retry
            </Button>
          )}
        >
          {error || 'Device not found'}
        </MuiAlert>

        <Button variant="outlined" onClick={() => navigate('/devices')}>
          Back to Devices
        </Button>
      </Box>
    );
  }

  return (
    <Box>
      <Breadcrumbs sx={{ mb: 2 }}>
        <Link component={RouterLink} to="/devices" color="inherit" underline="hover">
          Devices
        </Link>
        <Typography color="text.primary">{device.deviceName}</Typography>
      </Breadcrumbs>

      <Typography variant="body2" color="text.secondary" sx={{ mb: 2, maxWidth: 640 }}>
        Device profile below. Live readings and charts are tied to sensors —{' '}
        <Link component={RouterLink} to={`/sensors?deviceId=${device.deviceId}`} underline="hover">
          open Sensors for this device
        </Link>
        .
      </Typography>

      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12}>
          <Paper sx={{ ...panelSx }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2, flexWrap: 'wrap', gap: 1 }}>
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
                <Card variant="outlined" sx={{ boxShadow: 'none' }}>
                  <CardContent>
                    <Typography color="text.secondary" gutterBottom variant="body2">
                      Device ID
                    </Typography>
                    <Typography variant="h6">{device.deviceId}</Typography>
                  </CardContent>
                </Card>
              </Grid>

              <Grid item xs={12} md={6}>
                <Card variant="outlined" sx={{ boxShadow: 'none' }}>
                  <CardContent>
                    <Typography color="text.secondary" gutterBottom variant="body2">
                      Device Type
                    </Typography>
                    <Typography variant="h6">{device.deviceType}</Typography>
                  </CardContent>
                </Card>
              </Grid>

              {device.location && (
                <Grid item xs={12} md={6}>
                  <Card variant="outlined" sx={{ boxShadow: 'none' }}>
                    <CardContent>
                      <Typography color="text.secondary" gutterBottom variant="body2">
                        Location
                      </Typography>
                      <Typography variant="h6">{device.location}</Typography>
                    </CardContent>
                  </Card>
                </Grid>
              )}

              {device.facilityType && (
                <Grid item xs={12} md={6}>
                  <Card variant="outlined" sx={{ boxShadow: 'none' }}>
                    <CardContent>
                      <Typography color="text.secondary" gutterBottom variant="body2">
                        Facility Type
                      </Typography>
                      <Typography variant="h6">{device.facilityType}</Typography>
                    </CardContent>
                  </Card>
                </Grid>
              )}

              {device.edgeDeviceType && (
                <Grid item xs={12} md={6}>
                  <Card variant="outlined" sx={{ boxShadow: 'none' }}>
                    <CardContent>
                      <Typography color="text.secondary" gutterBottom variant="body2">
                        Edge Device Type
                      </Typography>
                      <Typography variant="h6">{device.edgeDeviceType}</Typography>
                    </CardContent>
                  </Card>
                </Grid>
              )}

              {device.edgeDeviceId && (
                <Grid item xs={12} md={6}>
                  <Card variant="outlined" sx={{ boxShadow: 'none' }}>
                    <CardContent>
                      <Typography color="text.secondary" gutterBottom variant="body2">
                        Edge Device ID
                      </Typography>
                      <Typography variant="h6">{device.edgeDeviceId}</Typography>
                    </CardContent>
                  </Card>
                </Grid>
              )}

              <Grid item xs={12} md={6}>
                <Card variant="outlined" sx={{ boxShadow: 'none' }}>
                  <CardContent>
                    <Typography color="text.secondary" gutterBottom variant="body2">
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
                <Card variant="outlined" sx={{ boxShadow: 'none' }}>
                  <CardContent>
                    <Typography color="text.secondary" gutterBottom variant="body2">
                      Created At
                    </Typography>
                    <Typography variant="h6">
                      {new Date(device.createdAt).toLocaleString()}
                    </Typography>
                  </CardContent>
                </Card>
              </Grid>

            </Grid>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
};

export default DeviceDetailPage;
