import React, { useEffect, useState } from 'react';
import { Box, Chip, Tooltip, IconButton, CircularProgress, Typography } from '@mui/material';
import { Refresh, CheckCircle, Error, Warning } from '@mui/icons-material';
import { apiService } from '../../services/api';

interface MqttStatus {
  status: string;
  host: string;
  port: number;
  accessible: boolean;
  mqttReady: boolean;
  timestamp: string;
}

interface MqttStatusIndicatorProps {
  compact?: boolean; // If true, shows only icon/chip. If false, shows full details
}

const MqttStatusIndicator: React.FC<MqttStatusIndicatorProps> = ({ compact = false }) => {
  const [status, setStatus] = useState<MqttStatus | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const checkStatus = async () => {
    setLoading(true);
    setError(null);
    try {
      const mqttStatus = await apiService.checkMqttBrokerStatus();
      setStatus(mqttStatus);
    } catch (err: any) {
      setError(err.message || 'Failed to check MQTT status');
      setStatus(null);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    checkStatus();
    // Auto-refresh every 30 seconds
    const interval = setInterval(checkStatus, 30000);
    return () => clearInterval(interval);
  }, []);

  const getStatusColor = () => {
    if (!status) return 'default';
    if (status.status === 'ready') return 'success';
    if (status.status === 'unavailable') return 'error';
    return 'warning';
  };

  const getStatusIcon = () => {
    if (loading) return <CircularProgress size={16} />;
    if (!status) return <Error fontSize="small" />;
    if (status.status === 'ready') return <CheckCircle fontSize="small" />;
    if (status.status === 'unavailable') return <Error fontSize="small" />;
    return <Warning fontSize="small" />;
  };

  const getStatusLabel = () => {
    if (loading) return 'Checking...';
    if (!status) return 'Unknown';
    if (status.status === 'ready') return 'MQTT Ready';
    if (status.status === 'unavailable') return 'MQTT Unavailable';
    if (status.status === 'port_open_but_mqtt_failed') return 'MQTT Port Open';
    return 'MQTT Error';
  };

  const getTooltipText = () => {
    if (error) return `Error: ${error}`;
    if (!status) return 'MQTT status unknown';
    return `Host: ${status.host}:${status.port}\n` +
           `Status: ${status.status}\n` +
           `Port Accessible: ${status.accessible ? 'Yes' : 'No'}\n` +
           `MQTT Ready: ${status.mqttReady ? 'Yes' : 'No'}\n` +
           `Last Check: ${new Date(status.timestamp).toLocaleTimeString()}`;
  };

  if (compact) {
    return (
      <Tooltip title={getTooltipText()} arrow>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
          <Chip
            icon={getStatusIcon()}
            label={getStatusLabel()}
            color={getStatusColor()}
            size="small"
            onClick={checkStatus}
            sx={{ cursor: 'pointer' }}
          />
          <IconButton
            size="small"
            onClick={checkStatus}
            disabled={loading}
            sx={{ ml: 0.5 }}
          >
            <Refresh fontSize="small" />
          </IconButton>
        </Box>
      </Tooltip>
    );
  }

  return (
    <Box sx={{ p: 1, bgcolor: 'background.paper', borderRadius: 1, border: 1, borderColor: 'divider' }}>
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          {getStatusIcon()}
          <Box>
            <Typography variant="subtitle2">MQTT Broker Status</Typography>
            <Typography variant="caption" color="textSecondary">
              {status ? `${status.host}:${status.port}` : 'Unknown'}
            </Typography>
          </Box>
        </Box>
        <IconButton size="small" onClick={checkStatus} disabled={loading}>
          <Refresh fontSize="small" />
        </IconButton>
      </Box>
      
      <Box>
        <Chip
          label={getStatusLabel()}
          color={getStatusColor()}
          size="small"
          sx={{ mb: 1 }}
        />
        {status && (
          <Box sx={{ mt: 1 }}>
            <Typography variant="caption" display="block" color="textSecondary">
              Port Accessible: {status.accessible ? '✓ Yes' : '✗ No'}
            </Typography>
            <Typography variant="caption" display="block" color="textSecondary">
              MQTT Protocol: {status.mqttReady ? '✓ Ready' : '✗ Failed'}
            </Typography>
            <Typography variant="caption" display="block" color="textSecondary">
              Last Check: {new Date(status.timestamp).toLocaleTimeString()}
            </Typography>
          </Box>
        )}
        {error && (
          <Typography variant="caption" color="error" display="block" sx={{ mt: 1 }}>
            {error}
          </Typography>
        )}
      </Box>
    </Box>
  );
};

export default MqttStatusIndicator;

