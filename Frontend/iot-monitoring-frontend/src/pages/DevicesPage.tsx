import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Typography,
  Button,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  Skeleton,
  Alert as MuiAlert,
} from '@mui/material';
import { alpha } from '@mui/material/styles';
import { Add as AddIcon } from '@mui/icons-material';
import { apiService } from '../services/api';
import DeviceForm from '../components/device/DeviceForm';
import { Device, DeviceList, CreateDevice } from '../types';

function deviceToFormValues(d: Device): CreateDevice {
  return {
    deviceName: d.deviceName,
    deviceType: d.deviceType,
    location: d.location ?? '',
    facilityType: d.facilityType ?? '',
    edgeDeviceType: d.edgeDeviceType ?? '',
    edgeDeviceId: d.edgeDeviceId ?? '',
    description: d.description ?? '',
  };
}

const panelSx = {
  p: 2.5,
  border: 1,
  borderColor: 'divider',
  boxShadow: 'none',
  bgcolor: 'background.paper',
};

/** Minimal ghost pills: transparent fill, thin outline (matches View reference). */
const tableActionGhostBaseSx = {
  minWidth: 48,
  px: 1.125,
  py: 0.45,
  borderRadius: '999px',
  fontSize: '0.8125rem',
  fontWeight: 500,
  textTransform: 'none' as const,
  lineHeight: 1.2,
  boxShadow: 'none',
  bgcolor: 'transparent',
  borderWidth: 1,
  borderStyle: 'solid',
};

const tableActionGhostSageSx = {
  ...tableActionGhostBaseSx,
  borderColor: '#8fad93',
  color: '#1e4d3a',
  '&:hover': {
    boxShadow: 'none',
    borderColor: '#6f9276',
    bgcolor: alpha('#8fad93', 0.12),
  },
};

const tableActionGhostDangerSx = {
  ...tableActionGhostBaseSx,
  borderColor: alpha('#b71c1c', 0.45),
  color: '#8b1a1a',
  '&:hover': {
    boxShadow: 'none',
    borderColor: alpha('#b71c1c', 0.65),
    bgcolor: alpha('#b71c1c', 0.06),
  },
};

const DevicesPage: React.FC = () => {
  const navigate = useNavigate();
  const [devices, setDevices] = useState<DeviceList[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [editingDevice, setEditingDevice] = useState<DeviceList | null>(null);
  const [formInitialData, setFormInitialData] = useState<CreateDevice | undefined>(undefined);

  useEffect(() => {
    loadDevices();
  }, []);

  const loadDevices = async () => {
    try {
      setLoading(true);
      setLoadError(null);
      const data = await apiService.getDevices();
      setDevices(data);
    } catch (error: any) {
      console.error('Error loading devices:', error.message || error);
      setDevices([]);
      setLoadError(error?.message || 'Failed to load devices');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmitDevice = async (device: CreateDevice) => {
    if (editingDevice) {
      await apiService.updateDevice(editingDevice.deviceId, device);
    } else {
      await apiService.createDevice(device);
    }
    await loadDevices();
  };

  const handleEditDevice = async (device: DeviceList) => {
    try {
      const full = await apiService.getDevice(device.deviceId);
      setEditingDevice(device);
      setFormInitialData(deviceToFormValues(full));
      setFormOpen(true);
    } catch (error: any) {
      console.error('Error loading device:', error);
      alert(error.message || 'Failed to load device for editing');
    }
  };

  const handleDeleteDevice = async (id: number) => {
    if (window.confirm('Are you sure you want to delete this device?')) {
      try {
        await apiService.deleteDevice(id);
        await loadDevices();
      } catch (error) {
        console.error('Error deleting device:', error);
        alert('Failed to delete device');
      }
    }
  };

  const handleCloseForm = () => {
    setFormOpen(false);
    setEditingDevice(null);
    setFormInitialData(undefined);
  };

  return (
    <Box>
      <Box
        sx={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: 1,
          mb: 2,
        }}
      >
        <Typography variant="h4" component="h1" gutterBottom sx={{ mb: 0 }}>
          Devices
        </Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => {
            setEditingDevice(null);
            setFormInitialData(undefined);
            setFormOpen(true);
          }}
        >
          Add Device
        </Button>
      </Box>

      <DeviceForm
        key={editingDevice ? `edit-${editingDevice.deviceId}` : 'create'}
        open={formOpen}
        onClose={handleCloseForm}
        onSubmit={handleSubmitDevice}
        initialData={formInitialData}
        title={editingDevice ? 'Edit Device' : 'Create Device'}
      />

      {loadError && (
        <MuiAlert
          severity="error"
          sx={{ mb: 3 }}
          action={(
            <Button color="inherit" size="small" onClick={loadDevices}>
              Retry
            </Button>
          )}
        >
          {loadError}
        </MuiAlert>
      )}

      <Paper sx={{ ...panelSx, mb: 3 }}>
        {loading ? (
          <Box>
            <Skeleton variant="rounded" height={40} sx={{ mb: 1 }} />
            <Skeleton variant="rounded" height={40} sx={{ mb: 1 }} />
            <Skeleton variant="rounded" height={40} sx={{ mb: 1 }} />
            <Skeleton variant="rounded" height={40} sx={{ mb: 1 }} />
            <Skeleton variant="rounded" height={40} />
          </Box>
        ) : (
          <TableContainer>
            <Table size="small" stickyHeader>
              <TableHead>
                <TableRow
                  sx={{
                    '& .MuiTableCell-head': {
                      bgcolor: 'background.default',
                      fontWeight: 600,
                    },
                  }}
                >
                  <TableCell>ID</TableCell>
                  <TableCell>Name</TableCell>
                  <TableCell>Type</TableCell>
                  <TableCell>Location</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Last Seen</TableCell>
                  <TableCell align="center">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {devices.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} align="center" sx={{ py: 4 }}>
                      <Typography color="text.secondary">No devices found</Typography>
                      <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.5 }}>
                        Add a device to get started
                      </Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  devices.map((device) => (
                    <TableRow key={device.deviceId} hover sx={{ '&:last-child td': { borderBottom: 0 } }}>
                      <TableCell>{device.deviceId}</TableCell>
                      <TableCell>{device.deviceName}</TableCell>
                      <TableCell>{device.deviceType}</TableCell>
                      <TableCell>{device.location || 'N/A'}</TableCell>
                      <TableCell>
                        <Chip
                          label={device.isActive ? 'Active' : 'Inactive'}
                          color={device.isActive ? 'success' : 'default'}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>
                        {device.lastSeenAt
                          ? new Date(device.lastSeenAt).toLocaleString()
                          : 'Never'}
                      </TableCell>
                      <TableCell>
                        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5, justifyContent: 'center', alignItems: 'center' }}>
                          <Button
                            size="small"
                            variant="outlined"
                            color="success"
                            onClick={() => navigate(`/devices/${device.deviceId}`)}
                            sx={tableActionGhostSageSx}
                          >
                            View
                          </Button>
                          <Button
                            size="small"
                            variant="outlined"
                            color="success"
                            onClick={() => handleEditDevice(device)}
                            sx={tableActionGhostSageSx}
                          >
                            Edit
                          </Button>
                          <Button
                            size="small"
                            variant="outlined"
                            color="error"
                            onClick={() => handleDeleteDevice(device.deviceId)}
                            sx={tableActionGhostDangerSx}
                          >
                            Delete
                          </Button>
                        </Box>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </Paper>
    </Box>
  );
};

export default DevicesPage;
