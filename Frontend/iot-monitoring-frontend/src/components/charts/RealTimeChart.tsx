import React, { useEffect, useState, useRef } from 'react';
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
  timeWindowMinutes?: number; // Time window in minutes (SCADA style)
}

const RealTimeChart: React.FC<RealTimeChartProps> = ({
  data,
  maxDataPoints = 50,
  name = 'Value',
  color = '#1976d2',
  height = 300,
  unit = '',
  timeWindowMinutes,
}) => {
  const [chartData, setChartData] = useState<Array<{ time: string; value: number; timestamp: number }>>([]);

  useEffect(() => {
    // SCADA-style rolling window behavior:
    // Option 1: Time-based window (most common in SCADA) - shows last N minutes
    // Option 2: Data points window - shows last N data points
    const now = Date.now();
    const sortedData = [...data]
      .sort((a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime())
      .map((point) => {
        const date = new Date(point.timestamp);
        const timestamp = date.getTime();
        // Format time based on window size - show seconds for small windows
        let timeLabel: string;
        if (timeWindowMinutes && timeWindowMinutes < 1) {
          // For windows less than 1 minute, show seconds
          const seconds = date.getSeconds();
          const minutes = date.getMinutes();
          timeLabel = `${minutes}:${seconds.toString().padStart(2, '0')}`;
        } else if (timeWindowMinutes && timeWindowMinutes < 5) {
          // For windows less than 5 minutes, show MM:SS
          const seconds = date.getSeconds();
          const minutes = date.getMinutes();
          timeLabel = `${minutes}:${seconds.toString().padStart(2, '0')}`;
        } else {
          // For larger windows, show full time
          timeLabel = date.toLocaleTimeString();
        }
        return {
          time: timeLabel,
          value: point.value,
          timestamp: timestamp,
        };
      })
      .filter((point) => {
        // If time window is specified, filter by time (SCADA style)
        if (timeWindowMinutes) {
          const windowMs = timeWindowMinutes * 60 * 1000;
          return (now - point.timestamp) <= windowMs;
        }
        // Otherwise, use data points limit
        return true;
      })
      .slice(-maxDataPoints); // Keep only the most recent data
    
    setChartData(sortedData);
  }, [data, maxDataPoints, timeWindowMinutes]);

  return (
    <ResponsiveContainer width="100%" height={height}>
      <RechartsLineChart 
        data={chartData}
        margin={{ top: 5, right: 20, left: 10, bottom: 5 }}
      >
        <CartesianGrid strokeDasharray="3 3" opacity={0.3} />
        <XAxis 
          dataKey="time"
          tick={{ fontSize: 12 }}
          interval={timeWindowMinutes && timeWindowMinutes < 1 ? 0 : 'preserveStartEnd'}
          // Show time labels - data is sorted oldest (left) to newest (right)
          // When window is full, oldest data scrolls out left, new data appears on right
          // For high resolution (seconds), show more ticks
        />
        <YAxis 
          label={{ value: unit, angle: -90, position: 'insideLeft' }}
          domain={['auto', 'auto']}
          tick={{ fontSize: 12 }}
        />
        <Tooltip
          formatter={(value: number) => [`${value.toFixed(2)} ${unit}`, name]}
          labelFormatter={(label) => `Time: ${label}`}
          contentStyle={{
            backgroundColor: 'rgba(255, 255, 255, 0.95)',
            border: '1px solid #ccc',
            borderRadius: '4px',
          }}
        />
        <Legend />
        <Line
          type="monotone"
          dataKey="value"
          name={name}
          stroke={color}
          strokeWidth={2.5}
          dot={false}
          isAnimationActive={true}
          animationDuration={300}
          connectNulls={true}
        />
      </RechartsLineChart>
    </ResponsiveContainer>
  );
};

export default RealTimeChart;

