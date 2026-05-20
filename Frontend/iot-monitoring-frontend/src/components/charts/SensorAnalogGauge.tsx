import React from 'react';
import { Box, Typography, Stack } from '@mui/material';

interface SensorAnalogGaugeProps {
  value: number | null;
  min?: number;
  max?: number;
  unit?: string;
  name?: string;
}

/** Semi-circular radial gauge with needle vs min–max range. */
const SensorAnalogGauge: React.FC<SensorAnalogGaugeProps> = ({
  value,
  min = 0,
  max = 100,
  unit = '',
  name = 'Value',
}) => {
  const cx = 100;
  const cy = 100;
  const rOuter = 78;
  const rTicks = 70;
  const needleLen = 62;

  let t = 0;
  let display = '—';
  if (value != null && Number.isFinite(value)) {
    display = unit ? `${value.toFixed(2)} ${unit}` : value.toFixed(2);
    const span = max - min;
    if (span !== 0) {
      t = ((value - min) / span);
    }
    t = Math.min(1, Math.max(0, t));
  }

  /**
   * Angle from +x axis, math CCW (y up). Min (t=0) → π (left), max (t=1) → 0 (right), mid → up.
   * Draw with screen y down: tip = (cx + L cos θ, cy − L sin θ).
   */
  const theta = Math.PI * (1 - t);
  const tipX = cx + needleLen * Math.cos(theta);
  const tipY = cy - needleLen * Math.sin(theta);

  /** Upper semicircle track: arc from left to right through the top. */
  const trackPath = `M ${cx - rOuter},${cy} A ${rOuter},${rOuter} 0 1 1 ${cx + rOuter},${cy}`;

  return (
    <Box sx={{ width: '100%', maxWidth: 420, mx: 'auto', py: 2 }}>
      <Typography variant="subtitle2" color="text.secondary" gutterBottom>
        {name}
      </Typography>
      <Typography variant="h4" fontWeight={700} sx={{ mb: 1 }}>
        {display}
      </Typography>

      <Box sx={{ position: 'relative', width: '100%', maxWidth: 280, mx: 'auto' }}>
        <svg
          viewBox="0 0 200 118"
          width="100%"
          height="auto"
          aria-hidden
          style={{ display: 'block', overflow: 'visible' }}
        >
          <defs>
            <linearGradient id="gaugeTrackGrad" x1="0%" y1="0%" x2="100%" y2="0%">
              <stop offset="0%" stopColor="#90a4ae" stopOpacity={0.35} />
              <stop offset="50%" stopColor="#78909c" stopOpacity={0.5} />
              <stop offset="100%" stopColor="#90a4ae" stopOpacity={0.35} />
            </linearGradient>
          </defs>

          {/* Background track */}
          <path
            d={trackPath}
            fill="none"
            stroke="url(#gaugeTrackGrad)"
            strokeWidth={14}
            strokeLinecap="round"
          />

          {/* Subtle inner ring */}
          <path
            d={`M ${cx - rOuter + 8},${cy} A ${rOuter - 8},${rOuter - 8} 0 1 1 ${cx + rOuter - 8},${cy}`}
            fill="none"
            stroke="rgba(0,0,0,0.08)"
            strokeWidth={1}
          />

          {/* Tick marks */}
          {[0, 0.25, 0.5, 0.75, 1].map((u) => {
            const deg = 180 - u * 180;
            const rad = (deg * Math.PI) / 180;
            const x1 = cx + rTicks * Math.cos(rad);
            const y1 = cy - rTicks * Math.sin(rad);
            const x2 = cx + (rTicks - 10) * Math.cos(rad);
            const y2 = cy - (rTicks - 10) * Math.sin(rad);
            return (
              <line
                key={u}
                x1={x1}
                y1={y1}
                x2={x2}
                y2={y2}
                stroke="rgba(0,0,0,0.35)"
                strokeWidth={2}
                strokeLinecap="round"
              />
            );
          })}

          {/* Needle — explicit tip so it sweeps along the upper arc (not into the bottom clip) */}
          <line
            x1={cx}
            y1={cy}
            x2={tipX}
            y2={tipY}
            stroke="#c62828"
            strokeWidth={3}
            strokeLinecap="round"
          />

          {/* Pivot cap */}
          <circle cx={cx} cy={cy} r={8} fill="#37474f" stroke="#eceff1" strokeWidth={2} />
          <circle cx={cx} cy={cy} r={3} fill="#cfd8dc" />
        </svg>
      </Box>

      <Stack direction="row" justifyContent="space-between" sx={{ mt: 0.75, px: { xs: 0, sm: 1 }, maxWidth: 280, mx: 'auto' }}>
        <Typography variant="caption" color="text.secondary">
          {min}{unit ? ` ${unit}` : ''}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {max}{unit ? ` ${unit}` : ''}
        </Typography>
      </Stack>
    </Box>
  );
};

export default SensorAnalogGauge;
