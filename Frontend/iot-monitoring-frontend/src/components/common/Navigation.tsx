import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { AppBar, Toolbar, Typography, Button, Box } from '@mui/material';
import {
  Dashboard as DashboardIcon,
  Devices as DevicesIcon,
  Sensors as SensorsIcon,
  Notifications as NotificationsIcon,
} from '@mui/icons-material';
import MqttStatusIndicator from './MqttStatusIndicator';

const Navigation: React.FC = () => {
  const location = useLocation();

  const navItems = [
    { path: '/', label: 'Dashboard', icon: <DashboardIcon /> },
    { path: '/devices', label: 'Devices', icon: <DevicesIcon /> },
    { path: '/sensors', label: 'Sensors', icon: <SensorsIcon /> },
    { path: '/alerts', label: 'Alerts', icon: <NotificationsIcon /> },
  ];

  return (
    <AppBar position="static">
      <Toolbar>
        <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
          IoT Monitoring System
        </Typography>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <MqttStatusIndicator compact={true} />
          <Box sx={{ display: 'flex', gap: 1 }}>
            {navItems.map((item) => (
              <Button
                key={item.path}
                component={Link}
                to={item.path}
                color="inherit"
                startIcon={item.icon}
                variant={location.pathname === item.path ? 'outlined' : 'text'}
                sx={{
                  borderColor: location.pathname === item.path ? 'white' : 'transparent',
                }}
              >
                {item.label}
              </Button>
            ))}
          </Box>
        </Box>
      </Toolbar>
    </AppBar>
  );
};

export default Navigation;

