import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { AppBar, Toolbar, Typography, Button, Box } from '@mui/material';
import {
  Dashboard as DashboardIcon,
  Devices as DevicesIcon,
  Sensors as SensorsIcon,
  Tune as TuneIcon,
  Notifications as NotificationsIcon,
} from '@mui/icons-material';

const Navigation: React.FC = () => {
  const location = useLocation();

  const navItems = [
    { path: '/', label: 'Dashboard', icon: <DashboardIcon /> },
    { path: '/devices', label: 'Devices', icon: <DevicesIcon /> },
    { path: '/sensors', label: 'Sensors', icon: <SensorsIcon /> },
    { path: '/actuators', label: 'Actuators', icon: <TuneIcon /> },
    { path: '/alerts', label: 'Alerts', icon: <NotificationsIcon /> },
  ];

  return (
    <AppBar position="static">
      <Toolbar sx={{ py: 0.5 }}>
        <Typography variant="h6" component="div" sx={{ flexGrow: 1, fontWeight: 700, letterSpacing: '-0.02em' }}>
          IoT Monitoring System
        </Typography>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
          {navItems.map((item) => {
            const active = location.pathname === item.path;
            return (
              <Button
                key={item.path}
                component={Link}
                to={item.path}
                color={active ? 'primary' : 'inherit'}
                startIcon={item.icon}
                variant={active ? 'outlined' : 'text'}
                sx={{
                  color: active ? 'primary.main' : 'text.secondary',
                  borderColor: active ? 'primary.main' : 'transparent',
                }}
              >
                {item.label}
              </Button>
            );
          })}
        </Box>
      </Toolbar>
    </AppBar>
  );
};

export default Navigation;

