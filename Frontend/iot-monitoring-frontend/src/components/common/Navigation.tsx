import React from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { AppBar, Toolbar, Typography, Button, Box, Chip, Divider } from '@mui/material';
import {
  Dashboard as DashboardIcon,
  Devices as DevicesIcon,
  Sensors as SensorsIcon,
  Tune as TuneIcon,
  Notifications as NotificationsIcon,
  Rule as RuleIcon,
  History as HistoryIcon,
  Logout as LogoutIcon,
  Person as PersonIcon,
} from '@mui/icons-material';
import authService from '../../services/authService';

const Navigation: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const currentUser = authService.getCurrentUser();

  const navItems = [
    { path: '/dashboard', label: 'Dashboard', icon: <DashboardIcon /> },
    { path: '/devices', label: 'Devices', icon: <DevicesIcon /> },
    { path: '/sensors', label: 'Sensors', icon: <SensorsIcon /> },
    { path: '/actuators', label: 'Actuators', icon: <TuneIcon /> },
    { path: '/alert-rules', label: 'Alert Rules', icon: <RuleIcon /> },
    { path: '/alerts', label: 'Alerts', icon: <NotificationsIcon /> },
    { path: '/command-history', label: 'Commands', icon: <HistoryIcon /> },
  ];

  const handleLogout = () => {
    authService.logout();
    navigate('/login', { replace: true });
  };

  return (
    <AppBar position="static">
      <Toolbar sx={{ py: 0.5 }}>
        <Typography variant="h6" component="div" sx={{ flexGrow: 1, fontWeight: 700, letterSpacing: '-0.02em' }}>
          IoT Monitoring System
        </Typography>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
          {navItems.map((item) => {
            const active = location.pathname === item.path || (item.path === '/dashboard' && location.pathname === '/');
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

          <Divider orientation="vertical" flexItem sx={{ mx: 1, borderColor: 'rgba(255,255,255,0.2)' }} />

          {currentUser && (
            <Chip
              icon={<PersonIcon />}
              label={`${currentUser.username} (${currentUser.role})`}
              size="small"
              sx={{ color: 'white', borderColor: 'rgba(255,255,255,0.4)', mr: 1 }}
              variant="outlined"
            />
          )}

          <Button
            color="inherit"
            startIcon={<LogoutIcon />}
            onClick={handleLogout}
            sx={{ color: 'text.secondary' }}
          >
            Logout
          </Button>
        </Box>
      </Toolbar>
    </AppBar>
  );
};

export default Navigation;

