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
}

const RealTimeChart: React.FC<RealTimeChartProps> = ({
  data,
  maxDataPoints = 50,
  name = 'Value',
  color = '#1976d2',
  height = 300,
  unit = '',
}) => {
  const [chartData, setChartData] = useState<Array<{ time: string; value: number }>>([]);

  useEffect(() => {
    // Keep only the last maxDataPoints
    const recentData = data.slice(-maxDataPoints).map((point) => ({
      time: new Date(point.timestamp).toLocaleTimeString(),
      value: point.value,
    }));
    setChartData(recentData);
  }, [data, maxDataPoints]);

  return (
    <ResponsiveContainer width="100%" height={height}>
      <RechartsLineChart data={chartData}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="time" />
        <YAxis label={{ value: unit, angle: -90, position: 'insideLeft' }} />
        <Tooltip
          formatter={(value: number) => [`${value} ${unit}`, name]}
          labelFormatter={(label) => `Time: ${label}`}
        />
        <Legend />
        <Line
          type="monotone"
          dataKey="value"
          name={name}
          stroke={color}
          strokeWidth={2}
          dot={false}
          isAnimationActive={false}
        />
      </RechartsLineChart>
    </ResponsiveContainer>
  );
};

export default RealTimeChart;

