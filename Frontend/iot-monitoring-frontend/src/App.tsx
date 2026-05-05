import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { ThemeProvider } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';
import { Container, Box } from '@mui/material';
import Navigation from './components/common/Navigation';
import Dashboard from './pages/Dashboard';
import DevicesPage from './pages/DevicesPage';
import DeviceDetailPage from './pages/DeviceDetailPage';
import SensorsPage from './pages/SensorsPage';
import ActuatorsPage from './pages/ActuatorsPage';
import AlertsPage from './pages/AlertsPage';
import { appTheme } from './theme';

function App() {
  return (
    <ThemeProvider theme={appTheme}>
      <CssBaseline />
      <Router
        future={{
          v7_startTransition: true,
          v7_relativeSplatPath: true,
        }}
      >
        <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh', bgcolor: 'background.default' }}>
          <Navigation />
          <Container maxWidth="xl" sx={{ mt: 3, mb: 4, flex: 1, px: { xs: 2, sm: 3 } }}>
            <Routes>
              <Route path="/" element={<Dashboard />} />
              <Route path="/devices" element={<DevicesPage />} />
              <Route path="/devices/:id" element={<DeviceDetailPage />} />
              <Route path="/sensors" element={<SensorsPage />} />
              <Route path="/actuators" element={<ActuatorsPage />} />
              <Route path="/alerts" element={<AlertsPage />} />
            </Routes>
          </Container>
        </Box>
      </Router>
    </ThemeProvider>
  );
}

export default App;

