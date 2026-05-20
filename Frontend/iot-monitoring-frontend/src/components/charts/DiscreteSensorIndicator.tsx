import React from 'react';
import { Box, Chip, Typography } from '@mui/material';

interface DiscreteSensorIndicatorProps {
  value: number | null;
  sensorName?: string;
  threshold?: number;
  /** When true, omit sensor name caption (parent already titles the dialog). */
  hideSensorHeading?: boolean;
}

/** ON/OFF style indicator for discrete (0/1) readings — value > threshold treated as ON. */
const DiscreteSensorIndicator: React.FC<DiscreteSensorIndicatorProps> = ({
  value,
  sensorName,
  threshold = 0.5,
  hideSensorHeading,
}) => {
  const on = value != null && value > threshold;

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 2, py: 3 }}>
      {sensorName && !hideSensorHeading ? (
        <Typography variant="subtitle1" fontWeight={600}>
          {sensorName}
        </Typography>
      ) : null}

      <Box
        sx={(theme) => ({
          width: 88,
          height: 88,
          borderRadius: '50%',
          boxShadow: `inset 0 0 20px rgba(0,0,0,0.25), 0 0 12px ${on ? theme.palette.success.main : theme.palette.grey[600]}`,
          backgroundColor: on ? theme.palette.success.main : theme.palette.grey[800],
          border: `4px solid ${on ? theme.palette.success.light : theme.palette.grey[700]}`,
        })}
      />

      <Chip
        label={on ? 'ON' : 'OFF'}
        color={on ? 'success' : 'default'}
        variant={on ? 'filled' : 'outlined'}
        sx={{ fontWeight: 700, minWidth: 72 }}
      />
      <Typography variant="body2" color="text.secondary">
        {`Reading: ${value != null ? String(value) : '—'} (threshold > ${threshold})`}
      </Typography>
    </Box>
  );
};

export default DiscreteSensorIndicator;
