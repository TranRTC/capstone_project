import React, { useMemo } from 'react';
import { LIVE_CLOCK_SKEW_MS, parseApiTimestampMs } from './chartTrim';
import { useTheme } from '@mui/material/styles';
import {
  LineChart as RechartsLineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';

interface DataPoint {
  timestamp: string;
  value: number;
}

interface RealTimeChartProps {
  data: DataPoint[];
  maxDataPoints?: number;
  name?: string;
  color?: string;
  height?: number;
  unit?: string;
  timeWindowMinutes?: number;
  /** When true, disables line animation (SCADA-style live updates). */
  isLive?: boolean;
  /** Fixed Y scale when both bounds are set on the sensor. */
  yDomain?: [number, number];
  /** Fixed live time window [minMs, maxMs] — SCADA scroll once the window is full. */
  xDomain?: [number, number];
  /** Client "now" for live filtering (align with parent trim). */
  referenceNow?: number;
}

const RealTimeChart: React.FC<RealTimeChartProps> = ({
  data,
  maxDataPoints = 50,
  name = 'Value',
  color,
  height = 300,
  unit = '',
  timeWindowMinutes,
  isLive = false,
  yDomain,
  xDomain,
  referenceNow,
}) => {
  const theme = useTheme();
  const lineColor = color ?? theme.palette.primary.main;

  const chartData = useMemo(() => {
    const now = referenceNow ?? Date.now();
    const sorted = [...data].sort(
      (a, b) => parseApiTimestampMs(a.timestamp) - parseApiTimestampMs(b.timestamp)
    );

    let filtered = sorted;
    // History ranges are already scoped by the API — only clip to xDomain during live scroll.
    if (xDomain && isLive) {
      const [xMin, xMax] = xDomain;
      filtered = sorted.filter((point) => {
        const t = parseApiTimestampMs(point.timestamp);
        if (Number.isNaN(t)) return false;
        return t >= xMin && t <= xMax + LIVE_CLOCK_SKEW_MS;
      });
    } else if (timeWindowMinutes) {
      const windowMs = timeWindowMinutes * 60 * 1000;
      const xMin = now - windowMs;
      filtered = sorted.filter((point) => {
        const t = parseApiTimestampMs(point.timestamp);
        if (Number.isNaN(t)) return false;
        return t >= xMin && t <= now;
      });
    }

    const limited =
      !isLive && !timeWindowMinutes && filtered.length > maxDataPoints
        ? filtered.slice(-maxDataPoints)
        : filtered;

    return limited.map((point) => ({
      timestampMs: parseApiTimestampMs(point.timestamp),
      value: point.value,
    }));
  }, [data, maxDataPoints, timeWindowMinutes, isLive, xDomain, referenceNow]);

  type XAxisDomain = [number, number] | ['dataMin', 'dataMax'];

  const xAxisDomain = useMemo((): XAxisDomain => {
    if (xDomain && isLive) return xDomain;
    // History: use the fetched window so points spread across the full 10m/1h/24h axis.
    if (!isLive && xDomain) return xDomain;
    if (!isLive && chartData.length > 0) {
      const dataMin = Math.min(...chartData.map((p) => p.timestampMs));
      const dataMax = Math.max(...chartData.map((p) => p.timestampMs));
      const pad = 3000;
      return [dataMin - pad, dataMax + pad];
    }
    if (xDomain) return xDomain;
    if (isLive && chartData.length === 1) {
      const t = chartData[0].timestampMs;
      return [t - 30_000, t + 5_000];
    }
    return ['dataMin', 'dataMax'];
  }, [xDomain, isLive, chartData]);

  const showSecondsOnAxis =
    timeWindowMinutes != null && timeWindowMinutes < 5;

  const formatAxisTick = (timestampMs: number) => {
    const date = new Date(timestampMs);
    if (showSecondsOnAxis) {
      return date.toLocaleTimeString(undefined, {
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
      });
    }
    return date.toLocaleTimeString(undefined, {
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const gridColor = theme.palette.divider;
  const axisColor = theme.palette.text.secondary;

  return (
    <ResponsiveContainer width="100%" height={height}>
      <RechartsLineChart
        data={chartData}
        margin={{ top: 12, right: 8, left: 4, bottom: 8 }}
      >
        <CartesianGrid
          stroke={gridColor}
          strokeDasharray="4 4"
          vertical={false}
          opacity={1}
        />
        <XAxis
          type="number"
          dataKey="timestampMs"
          scale="time"
          domain={xAxisDomain}
          allowDataOverflow={false}
          tick={{ fill: axisColor, fontSize: 11 }}
          tickLine={false}
          axisLine={{ stroke: gridColor }}
          tickFormatter={formatAxisTick}
          dy={6}
        />
        <YAxis
          label={{
            value: unit || 'Value',
            angle: -90,
            position: 'insideLeft',
            style: { fill: axisColor, fontSize: 11, fontWeight: 500 },
            offset: 4,
          }}
          domain={yDomain ?? ['auto', 'auto']}
          tick={{ fill: axisColor, fontSize: 11 }}
          tickLine={false}
          axisLine={false}
          width={56}
        />
        <Tooltip
          formatter={(value: number) => [`${value.toFixed(2)}${unit ? ` ${unit}` : ''}`, name]}
          labelFormatter={(_, payload) => {
            const row = payload?.[0]?.payload as { timestampMs?: number } | undefined;
            if (row?.timestampMs != null) {
              return new Date(row.timestampMs).toLocaleString();
            }
            return '';
          }}
          cursor={{ stroke: gridColor, strokeWidth: 1 }}
          contentStyle={{
            backgroundColor: theme.palette.background.paper,
            border: `1px solid ${theme.palette.divider}`,
            borderRadius: Number(theme.shape.borderRadius),
            boxShadow: theme.shadows[3],
            fontSize: 12,
            padding: '10px 12px',
          }}
          labelStyle={{ color: theme.palette.text.secondary, marginBottom: 4, fontSize: 11 }}
          itemStyle={{ color: theme.palette.text.primary, fontWeight: 600 }}
        />
        <Legend
          wrapperStyle={{ paddingTop: 16 }}
          iconType="plainline"
          formatter={(value) => (
            <span style={{ color: theme.palette.text.primary, fontSize: 12, fontWeight: 500 }}>{value}</span>
          )}
        />
        <Line
          type={isLive ? 'linear' : 'monotone'}
          dataKey="value"
          name={name}
          stroke={lineColor}
          strokeWidth={2}
          dot={chartData.length < 2 ? { r: 3, fill: lineColor, strokeWidth: 0 } : false}
          activeDot={{ r: 4, strokeWidth: 0, fill: lineColor }}
          isAnimationActive={false}
          animationDuration={0}
          animationEasing="ease-out"
          connectNulls={false}
        />
      </RechartsLineChart>
    </ResponsiveContainer>
  );
};

export default RealTimeChart;
