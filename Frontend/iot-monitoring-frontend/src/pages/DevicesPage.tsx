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
  IconButton,
} from '@mui/material';
import { Add as AddIcon, Edit as EditIcon, Delete as DeleteIcon, Visibility as VisibilityIcon } from '@mui/icons-material';
import { apiService } from '../services/api';
import DeviceForm from '../components/device/DeviceForm';
import { DeviceList, CreateDevice } from '../types';

const DevicesPage: React.FC = () => {
  const navigate = useNavigate();
  const [devices, setDevices] = useState<DeviceList[]>([]);
  const [loading, setLoading] = useState(true);
  const [formOpen, setFormOpen] = useState(false);
  const [editingDevice, setEditingDevice] = useState<DeviceList | null>(null);

  useEffect(() => {
    loadDevices();
  }, []);

  const loadDevices = async () => {
    try {
      setLoading(true);
      const data = await apiService.getDevices();
      setDevices(data);
    } catch (error: any) {
      console.error('Error loading devices:', error.message || error);
      setDevices([]);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateDevice = async (device: CreateDevice) => {
    await apiService.createDevice(device);
    await loadDevices();
  };

  const handleEditDevice = (device: DeviceList) => {
    setEditingDevice(device);
    setFormOpen(true);
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
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3 }}>
        <Typography variant="h4" component="h1">
          Devices
        </Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => setFormOpen(true)}
        >
          Add Device
        </Button>
      </Box>

      <DeviceForm
        open={formOpen}
        onClose={handleCloseForm}
        onSubmit={handleCreateDevice}
        title={editingDevice ? 'Edit Device' : 'Create Device'}
      />

      {loading ? (
        <Typography>Loading...</Typography>
      ) : (
        <>
          <TableContainer component={Paper} sx={{ mb: 4 }}>
            <Table>
              <TableHead>
                  <TableRow>
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
                  <TableCell colSpan={8} align="center">
                    No devices found
                  </TableCell>
                </TableRow>
                ) : (
                  devices.map((device) => (
                    <TableRow key={device.deviceId}>
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
                        <Box sx={{ display: 'flex', gap: 1, justifyContent: 'center' }}>
                          <IconButton
                            size="small"
                            onClick={() => navigate(`/devices/${device.deviceId}`)}
                            color="primary"
                            title="View Details"
                          >
                            <VisibilityIcon />
                          </IconButton>
                          <IconButton
                            size="small"
                            onClick={() => handleEditDevice(device)}
                            color="primary"
                            title="Edit Device"
                          >
                            <EditIcon />
                          </IconButton>
                          <IconButton
                            size="small"
                            onClick={() => handleDeleteDevice(device.deviceId)}
                            color="error"
                            title="Delete Device"
                          >
                            <DeleteIcon />
                          </IconButton>
                        </Box>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </>
      )}
    </Box>
  );
};

export default DevicesPage;

