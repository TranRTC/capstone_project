import React from 'react';
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
  name?: string;
}

interface LineChartProps {
  data: DataPoint[];
  dataKey?: string;
  name?: string;
  color?: string;
  height?: number;
}

const LineChart: React.FC<LineChartProps> = ({
  data,
  dataKey = 'value',
  name = 'Value',
  color = '#1976d2',
  height = 300,
}) => {
  // Format data for chart
  const chartData = data.map((point) => ({
    ...point,
    time: new Date(point.timestamp).toLocaleTimeString(),
  }));

  return (
    <ResponsiveContainer width="100%" height={height}>
      <RechartsLineChart data={chartData}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="time" />
        <YAxis />
        <Tooltip />
        <Legend />
        <Line
          type="monotone"
          dataKey={dataKey}
          name={name}
          stroke={color}
          strokeWidth={2}
          dot={{ r: 4 }}
        />
      </RechartsLineChart>
    </ResponsiveContainer>
  );
};

export default LineChart;

