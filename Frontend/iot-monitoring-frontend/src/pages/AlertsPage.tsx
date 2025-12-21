import React, { useEffect, useState } from 'react';
import { Box, Typography, Paper, Chip, Button, Stack } from '@mui/material';
import { apiService } from '../services/api';
import { signalRService } from '../services/signalRService';
import { Alert } from '../types';

const AlertsPage: React.FC = () => {
  const [alerts, setAlerts] = useState<Alert[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadAlerts();
    connectSignalR();

    return () => {
      signalRService.off('NewAlert');
      signalRService.off('AlertResolved');
    };
  }, []);

  const loadAlerts = async () => {
    try {
      setLoading(true);
      const data = await apiService.getActiveAlerts();
      setAlerts(data);
    } catch (error) {
      console.error('Error loading alerts:', error);
    } finally {
      setLoading(false);
    }
  };

  const connectSignalR = async () => {
    try {
      await signalRService.start();
      await signalRService.subscribeToAlerts();

      signalRService.onNewAlert((alert) => {
        setAlerts((prev) => [alert, ...prev]);
      });

      signalRService.onAlertResolved((alert) => {
        setAlerts((prev) => prev.filter((a) => a.alertId !== alert.alertId));
      });
    } catch (error) {
      console.error('SignalR connection error:', error);
    }
  };

  const handleAcknowledge = async (alertId: number) => {
    try {
      await apiService.acknowledgeAlert(alertId);
      await loadAlerts();
    } catch (error) {
      console.error('Error acknowledging alert:', error);
    }
  };

  const handleResolve = async (alertId: number) => {
    try {
      await apiService.resolveAlert(alertId);
      await loadAlerts();
    } catch (error) {
      console.error('Error resolving alert:', error);
    }
  };

  const getSeverityColor = (severity: string) => {
    switch (severity.toLowerCase()) {
      case 'critical':
        return 'error';
      case 'high':
        return 'warning';
      case 'medium':
        return 'info';
      default:
        return 'default';
    }
  };

  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom>
        Alerts
      </Typography>

      {loading ? (
        <Typography>Loading...</Typography>
      ) : alerts.length === 0 ? (
        <Paper sx={{ p: 3 }}>
          <Typography>No active alerts</Typography>
        </Paper>
      ) : (
        <Box>
          {alerts.map((alert) => (
            <Paper key={alert.alertId} sx={{ p: 2, mb: 2 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start' }}>
                <Box sx={{ flex: 1 }}>
                  <Typography variant="h6">{alert.message}</Typography>
                  <Typography variant="body2" color="textSecondary">
                    Device {alert.deviceId}
                    {alert.sensorId && ` • Sensor ${alert.sensorId}`}
                    {alert.triggerValue !== null && ` • Value: ${alert.triggerValue}`}
                  </Typography>
                  <Typography variant="caption" color="textSecondary" display="block" sx={{ mt: 0.5 }}>
                    Triggered: {new Date(alert.triggeredAt).toLocaleString()}
                    {alert.acknowledgedAt &&
                      ` • Acknowledged: ${new Date(alert.acknowledgedAt).toLocaleString()}`}
                  </Typography>
                </Box>
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1, alignItems: 'flex-end' }}>
                  <Chip
                    label={alert.severity}
                    color={getSeverityColor(alert.severity) as any}
                    size="small"
                  />
                  <Stack direction="row" spacing={1}>
                    {!alert.acknowledgedAt && (
                      <Button
                        size="small"
                        variant="outlined"
                        onClick={() => handleAcknowledge(alert.alertId)}
                      >
                        Acknowledge
                      </Button>
                    )}
                    {alert.status === 'Active' && (
                      <Button
                        size="small"
                        variant="contained"
                        color="success"
                        onClick={() => handleResolve(alert.alertId)}
                      >
                        Resolve
                      </Button>
                    )}
                  </Stack>
                </Box>
              </Box>
            </Paper>
          ))}
        </Box>
      )}
    </Box>
  );
};

export default AlertsPage;

