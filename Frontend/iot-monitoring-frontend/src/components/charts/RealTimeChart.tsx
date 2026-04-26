import React, { useEffect, useState } from 'react';
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
}

const RealTimeChart: React.FC<RealTimeChartProps> = ({
  data,
  maxDataPoints = 50,
  name = 'Value',
  color,
  height = 300,
  unit = '',
  timeWindowMinutes,
}) => {
  const theme = useTheme();
  const lineColor = color ?? theme.palette.primary.main;
  const [chartData, setChartData] = useState<Array<{ time: string; value: number; timestamp: number }>>([]);

  useEffect(() => {
    const now = Date.now();
    const sortedData = [...data]
      .sort((a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime())
      .map((point) => {
        const date = new Date(point.timestamp);
        const timestamp = date.getTime();
        let timeLabel: string;
        if (timeWindowMinutes && timeWindowMinutes < 1) {
          const seconds = date.getSeconds();
          const minutes = date.getMinutes();
          timeLabel = `${minutes}:${seconds.toString().padStart(2, '0')}`;
        } else if (timeWindowMinutes && timeWindowMinutes < 5) {
          const seconds = date.getSeconds();
          const minutes = date.getMinutes();
          timeLabel = `${minutes}:${seconds.toString().padStart(2, '0')}`;
        } else {
          timeLabel = date.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit', second: '2-digit' });
        }
        return {
          time: timeLabel,
          value: point.value,
          timestamp,
        };
      })
      .filter((point) => {
        if (timeWindowMinutes) {
          const windowMs = timeWindowMinutes * 60 * 1000;
          return now - point.timestamp <= windowMs;
        }
        return true;
      })
      .slice(-maxDataPoints);

    setChartData(sortedData);
  }, [data, maxDataPoints, timeWindowMinutes]);

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
          dataKey="time"
          tick={{ fill: axisColor, fontSize: 11 }}
          tickLine={false}
          axisLine={{ stroke: gridColor }}
          interval={timeWindowMinutes && timeWindowMinutes < 1 ? 0 : 'preserveStartEnd'}
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
          domain={['auto', 'auto']}
          tick={{ fill: axisColor, fontSize: 11 }}
          tickLine={false}
          axisLine={false}
          width={56}
        />
        <Tooltip
          formatter={(value: number) => [`${value.toFixed(2)}${unit ? ` ${unit}` : ''}`, name]}
          labelFormatter={(label) => label}
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
          type="monotone"
          dataKey="value"
          name={name}
          stroke={lineColor}
          strokeWidth={2}
          dot={false}
          activeDot={{ r: 4, strokeWidth: 0, fill: lineColor }}
          isAnimationActive
          animationDuration={400}
          animationEasing="ease-out"
          connectNulls
        />
      </RechartsLineChart>
    </ResponsiveContainer>
  );
};

export default RealTimeChart;
