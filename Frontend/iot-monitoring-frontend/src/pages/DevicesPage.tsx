import React, { useEffect, useState } from 'react';
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
import { Add as AddIcon, Edit as EditIcon, Delete as DeleteIcon } from '@mui/icons-material';
import { apiService } from '../services/api';
import DeviceForm from '../components/device/DeviceForm';
import { DeviceList, CreateDevice } from '../types';

const DevicesPage: React.FC = () => {
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
    } catch (error) {
      console.error('Error loading devices:', error);
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
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>ID</TableCell>
                <TableCell>Name</TableCell>
                <TableCell>Type</TableCell>
                <TableCell>Location</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Last Seen</TableCell>
                <TableCell>Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {devices.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={7} align="center">
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
                      <IconButton
                        size="small"
                        onClick={() => handleEditDevice(device)}
                        color="primary"
                      >
                        <EditIcon />
                      </IconButton>
                      <IconButton
                        size="small"
                        onClick={() => handleDeleteDevice(device.deviceId)}
                        color="error"
                      >
                        <DeleteIcon />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Box>
  );
};

export default DevicesPage;

