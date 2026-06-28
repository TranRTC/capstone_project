import React, { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import {
  AppBar,
  Toolbar,
  Typography,
  Button,
  Box,
  Divider,
  IconButton,
  Menu,
  MenuItem,
  ListItemIcon,
  ListItemText,
} from '@mui/material';
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
  ManageAccounts as ManageAccountsIcon,
  SmartToy as SmartToyIcon,
} from '@mui/icons-material';
import authService from '../../services/authService';

const Navigation: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const currentUser = authService.getCurrentUser();
  const [accountAnchor, setAccountAnchor] = useState<null | HTMLElement>(null);
  const accountMenuOpen = Boolean(accountAnchor);

  const isAdmin = currentUser?.role === 'Admin';

  const navItems = [
    { path: '/dashboard', label: 'Dashboard', icon: <DashboardIcon />, adminOnly: false },
    { path: '/devices', label: 'Devices', icon: <DevicesIcon />, adminOnly: false },
    { path: '/sensors', label: 'Sensors', icon: <SensorsIcon />, adminOnly: false },
    { path: '/actuators', label: 'Actuators', icon: <TuneIcon />, adminOnly: false },
    { path: '/alert-rules', label: 'Alert Rules', icon: <RuleIcon />, adminOnly: false },
    { path: '/alerts', label: 'Alerts', icon: <NotificationsIcon />, adminOnly: false },
    { path: '/command-history', label: 'Commands', icon: <HistoryIcon />, adminOnly: false },
    { path: '/assistant', label: 'Assistant', icon: <SmartToyIcon />, adminOnly: false },
    { path: '/users', label: 'Users', icon: <ManageAccountsIcon />, adminOnly: true },
  ].filter((item) => !item.adminOnly || isAdmin);

  const handleLogout = () => {
    setAccountAnchor(null);
    authService.logout();
    navigate('/login', { replace: true });
  };

  return (
    <AppBar position="static">
      <Toolbar sx={{ py: 0.5, gap: 0.5, flexWrap: 'wrap' }}>
        <Typography variant="h6" component="div" sx={{ flexGrow: 1, fontWeight: 700, letterSpacing: '-0.02em' }}>
          IoT Dashboard
        </Typography>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, flexWrap: 'wrap' }}>
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
            <>
              <IconButton
                color="inherit"
                onClick={(e) => setAccountAnchor(e.currentTarget)}
                aria-label="Account menu"
                aria-controls={accountMenuOpen ? 'account-menu' : undefined}
                aria-haspopup="true"
                aria-expanded={accountMenuOpen ? 'true' : undefined}
                sx={{ color: 'text.secondary' }}
              >
                <PersonIcon />
              </IconButton>
              <Menu
                id="account-menu"
                anchorEl={accountAnchor}
                open={accountMenuOpen}
                onClose={() => setAccountAnchor(null)}
                anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
                transformOrigin={{ vertical: 'top', horizontal: 'right' }}
              >
                <MenuItem disabled sx={{ opacity: 1 }}>
                  <ListItemText
                    primary={currentUser.username}
                    secondary={currentUser.role}
                    primaryTypographyProps={{ fontWeight: 600 }}
                  />
                </MenuItem>
                <Divider />
                <MenuItem onClick={handleLogout}>
                  <ListItemIcon>
                    <LogoutIcon fontSize="small" />
                  </ListItemIcon>
                  <ListItemText>Logout</ListItemText>
                </MenuItem>
              </Menu>
            </>
          )}
        </Box>
      </Toolbar>
    </AppBar>
  );
};

export default Navigation;

