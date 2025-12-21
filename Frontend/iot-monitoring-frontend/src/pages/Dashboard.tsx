import React, { useEffect, useState } from 'react';
import { Grid, Paper, Typography, Box, Card, CardContent } from '@mui/material';
import * as signalR from '@microsoft/signalr';
import { apiService } from '../services/api';
import { signalRService } from '../services/signalRService';
import { DeviceList, Alert, SensorReading } from '../types';
import MqttStatusIndicator from '../components/common/MqttStatusIndicator';

const Dashboard: React.FC = () => {
  const [devices, setDevices] = useState<DeviceList[]>([]);
  const [activeAlerts, setActiveAlerts] = useState<Alert[]>([]);
  const [latestReading, setLatestReading] = useState<SensorReading | null>(null);
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    loadData();
    connectSignalR();

    return () => {
      signalRService.stop();
    };
  }, []);

  const loadData = async () => {
    try {
      const [devicesData, alertsData] = await Promise.all([
        apiService.getDevices(),
        apiService.getActiveAlerts(),
      ]);
      setDevices(devicesData);
      setActiveAlerts(alertsData);
    } catch (error: any) {
      console.error('Error loading dashboard data:', error.message || error);
      // Set empty arrays on error to prevent UI issues
      setDevices([]);
      setActiveAlerts([]);
    }
  };

  const connectSignalR = async () => {
    try {
      await signalRService.start();
      
      // Wait a moment to ensure connection is stable
      await new Promise(resolve => setTimeout(resolve, 100));
      
      const state = signalRService.getConnectionState();
      if (state === signalR.HubConnectionState.Connected) {
        setIsConnected(true);

        await signalRService.subscribeToAllDevices();
        await signalRService.subscribeToAlerts();

        signalRService.onSensorReading((reading) => {
          setLatestReading(reading);
        });

        signalRService.onNewAlert((alert) => {
          setActiveAlerts((prev) => [alert, ...prev]);
        });

        signalRService.onAlertResolved((alert) => {
          setActiveAlerts((prev) => prev.filter((a) => a.alertId !== alert.alertId));
        });
      } else {
        setIsConnected(false);
      }
    } catch (error: any) {
      // Ignore AbortError and negotiation errors - automatic reconnect will handle them
      // These are transient issues that resolve automatically
      if (error?.name !== 'AbortError' && 
          !(error?.message && error.message.includes('negotiation'))) {
        console.error('SignalR connection error:', error);
      }
      // Don't set connected to false immediately - let automatic reconnect try
      // The connection state will update when it successfully connects
    }
  };

  const activeDevices = devices.filter((d) => d.isActive).length;
  const totalDevices = devices.length;

  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom>
        Dashboard
      </Typography>

      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={6}>
          <Box sx={{ p: 1, bgcolor: isConnected ? 'success.light' : 'error.light', borderRadius: 1 }}>
            <Typography variant="body2">
              SignalR: {isConnected ? 'Connected' : 'Disconnected'}
            </Typography>
          </Box>
        </Grid>
        <Grid item xs={12} sm={6}>
          <MqttStatusIndicator compact={false} />
        </Grid>
      </Grid>

      <Grid container spacing={3}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                Total Devices
              </Typography>
              <Typography variant="h4">{totalDevices}</Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                Active Devices
              </Typography>
              <Typography variant="h4">{activeDevices}</Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                Active Alerts
              </Typography>
              <Typography variant="h4" color="error">
                {activeAlerts.length}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                Latest Reading
              </Typography>
              <Typography variant="h4">
                {latestReading ? `${latestReading.value}` : 'N/A'}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom>
              Recent Alerts
            </Typography>
            {activeAlerts.length === 0 ? (
              <Typography color="textSecondary">No active alerts</Typography>
            ) : (
              <Box>
                {activeAlerts.slice(0, 5).map((alert) => (
                  <Box key={alert.alertId} sx={{ mb: 1, p: 1, bgcolor: 'background.default' }}>
                    <Typography variant="body2">
                      <strong>{alert.severity}:</strong> {alert.message}
                    </Typography>
                    <Typography variant="caption" color="textSecondary">
                      Device {alert.deviceId} • {new Date(alert.triggeredAt).toLocaleString()}
                    </Typography>
                  </Box>
                ))}
              </Box>
            )}
          </Paper>
        </Grid>

        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom>
              Latest Sensor Reading
            </Typography>
            {latestReading ? (
              <Box>
                <Typography variant="body1">
                  <strong>Value:</strong> {latestReading.value}
                </Typography>
                <Typography variant="body2" color="textSecondary">
                  Device {latestReading.deviceId} • Sensor {latestReading.sensorId}
                </Typography>
                <Typography variant="caption" color="textSecondary">
                  {new Date(latestReading.timestamp).toLocaleString()}
                </Typography>
              </Box>
            ) : (
              <Typography color="textSecondary">No readings yet</Typography>
            )}
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
};

export default Dashboard;

