import React, { useEffect, useState, useCallback } from 'react';
import {
  Box,
  Typography,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Button,
  Chip,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Alert,
  Tooltip,
  CircularProgress,
  Stack,
} from '@mui/material';
import {
  Add as AddIcon,
  PersonOff as DeactivateIcon,
  Refresh as RefreshIcon,
} from '@mui/icons-material';
import authService from '../services/authService';
import { runtimeConfig } from '../config/runtimeConfig';
import axios from 'axios';

interface UserRow {
  userId: number;
  username: string;
  email?: string;
  role: string;
  isActive: boolean;
  createdAt: string;
}

interface CreateUserForm {
  username: string;
  email: string;
  password: string;
  role: string;
}

const ROLES = ['Admin', 'Operator', 'Viewer'];

const authApi = axios.create({ baseURL: runtimeConfig.apiBaseUrl });
authApi.interceptors.request.use((config) => {
  const token = authService.getToken();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

const UserManagementPage: React.FC = () => {
  const currentUser = authService.getCurrentUser();
  const [users, setUsers] = useState<UserRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);

  // Create dialog state
  const [createOpen, setCreateOpen] = useState(false);
  const [createForm, setCreateForm] = useState<CreateUserForm>({
    username: '', email: '', password: '', role: 'Viewer',
  });
  const [createError, setCreateError] = useState<string | null>(null);
  const [createLoading, setCreateLoading] = useState(false);

  // Role change in-progress tracker
  const [roleChanging, setRoleChanging] = useState<number | null>(null);

  // Deactivate confirm dialog
  const [deactivateTarget, setDeactivateTarget] = useState<UserRow | null>(null);
  const [deactivateLoading, setDeactivateLoading] = useState(false);

  const loadUsers = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await authApi.get<UserRow[]>('/auth/users');
      setUsers(response.data);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load users.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadUsers();
  }, [loadUsers]);

  const handleRoleChange = async (user: UserRow, newRole: string) => {
    if (user.role === newRole) return;
    setRoleChanging(user.userId);
    try {
      await authApi.patch(`/auth/users/${user.userId}/role`, { role: newRole });
      setUsers((prev) => prev.map((u) => u.userId === user.userId ? { ...u, role: newRole } : u));
      setSuccessMsg(`${user.username}'s role updated to ${newRole}.`);
      setTimeout(() => setSuccessMsg(null), 3000);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to update role.');
    } finally {
      setRoleChanging(null);
    }
  };

  const handleDeactivate = async () => {
    if (!deactivateTarget) return;
    setDeactivateLoading(true);
    try {
      await authApi.delete(`/auth/users/${deactivateTarget.userId}`);
      setUsers((prev) => prev.map((u) =>
        u.userId === deactivateTarget.userId ? { ...u, isActive: false } : u
      ));
      setSuccessMsg(`${deactivateTarget.username} has been deactivated.`);
      setTimeout(() => setSuccessMsg(null), 3000);
      setDeactivateTarget(null);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to deactivate user.');
    } finally {
      setDeactivateLoading(false);
    }
  };

  const handleCreateUser = async () => {
    setCreateError(null);
    if (!createForm.username.trim() || !createForm.password) {
      setCreateError('Username and password are required.');
      return;
    }
    if (createForm.password.length < 8) {
      setCreateError('Password must be at least 8 characters.');
      return;
    }
    setCreateLoading(true);
    try {
      const response = await authApi.post<UserRow>('/auth/users', {
        username: createForm.username.trim(),
        email: createForm.email.trim() || undefined,
        password: createForm.password,
        role: createForm.role,
      });
      setUsers((prev) => [...prev, response.data]);
      setSuccessMsg(`User "${createForm.username}" created successfully.`);
      setTimeout(() => setSuccessMsg(null), 3000);
      setCreateOpen(false);
      setCreateForm({ username: '', email: '', password: '', role: 'Viewer' });
    } catch (err: any) {
      setCreateError(err.response?.data?.message || 'Failed to create user.');
    } finally {
      setCreateLoading(false);
    }
  };

  const formatDate = (iso: string) =>
    new Date(iso).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });

  return (
    <Box>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 3 }}>
        <Box>
          <Typography variant="h5" fontWeight={700}>User Management</Typography>
          <Typography variant="body2" color="text.secondary">
            Manage user accounts and roles. Admin only.
          </Typography>
        </Box>
        <Stack direction="row" spacing={1}>
          <Tooltip title="Refresh">
            <IconButton onClick={loadUsers} disabled={loading}>
              <RefreshIcon />
            </IconButton>
          </Tooltip>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setCreateOpen(true)}
          >
            Add User
          </Button>
        </Stack>
      </Box>

      {error && <Alert severity="error" onClose={() => setError(null)} sx={{ mb: 2 }}>{error}</Alert>}
      {successMsg && <Alert severity="success" onClose={() => setSuccessMsg(null)} sx={{ mb: 2 }}>{successMsg}</Alert>}

      {/* Users Table */}
      <TableContainer component={Paper} elevation={0} sx={{ border: 1, borderColor: 'divider' }}>
        <Table>
          <TableHead>
            <TableRow sx={{ bgcolor: 'grey.50' }}>
              <TableCell><strong>Username</strong></TableCell>
              <TableCell><strong>Email</strong></TableCell>
              <TableCell><strong>Role</strong></TableCell>
              <TableCell><strong>Status</strong></TableCell>
              <TableCell><strong>Created</strong></TableCell>
              <TableCell align="center"><strong>Actions</strong></TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={6} align="center" sx={{ py: 4 }}>
                  <CircularProgress size={28} />
                </TableCell>
              </TableRow>
            ) : users.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} align="center" sx={{ py: 4, color: 'text.secondary' }}>
                  No users found.
                </TableCell>
              </TableRow>
            ) : (
              users.map((user) => {
                const isSelf = user.username === currentUser?.username;
                return (
                  <TableRow
                    key={user.userId}
                    sx={{ opacity: user.isActive ? 1 : 0.5, '&:hover': { bgcolor: 'action.hover' } }}
                  >
                    <TableCell>
                      <Typography fontWeight={600}>{user.username}</Typography>
                      {isSelf && (
                        <Typography variant="caption" color="primary">(you)</Typography>
                      )}
                    </TableCell>
                    <TableCell>{user.email || <Typography variant="body2" color="text.disabled">—</Typography>}</TableCell>
                    <TableCell>
                      {isSelf ? (
                        <Chip
                          label={user.role}
                          size="small"
                          color={user.role === 'Admin' ? 'primary' : 'default'}
                        />
                      ) : (
                        <FormControl size="small" disabled={!user.isActive || roleChanging === user.userId}>
                          <Select
                            value={user.role}
                            onChange={(e) => handleRoleChange(user, e.target.value)}
                            sx={{ minWidth: 100 }}
                          >
                            {ROLES.map((r) => (
                              <MenuItem key={r} value={r}>{r}</MenuItem>
                            ))}
                          </Select>
                        </FormControl>
                      )}
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={user.isActive ? 'Active' : 'Inactive'}
                        color={user.isActive ? 'success' : 'default'}
                        size="small"
                      />
                    </TableCell>
                    <TableCell>{formatDate(user.createdAt)}</TableCell>
                    <TableCell align="center">
                      {!isSelf && user.isActive && (
                        <Tooltip title="Deactivate user">
                          <IconButton
                            size="small"
                            color="warning"
                            onClick={() => setDeactivateTarget(user)}
                          >
                            <DeactivateIcon />
                          </IconButton>
                        </Tooltip>
                      )}
                    </TableCell>
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Create User Dialog */}
      <Dialog open={createOpen} onClose={() => { setCreateOpen(false); setCreateError(null); }} maxWidth="sm" fullWidth>
        <DialogTitle>Add New User</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {createError && <Alert severity="error">{createError}</Alert>}
            <TextField
              label="Username"
              value={createForm.username}
              onChange={(e) => setCreateForm({ ...createForm, username: e.target.value })}
              required
              fullWidth
              autoFocus
            />
            <TextField
              label="Email (optional)"
              type="email"
              value={createForm.email}
              onChange={(e) => setCreateForm({ ...createForm, email: e.target.value })}
              fullWidth
            />
            <TextField
              label="Password"
              type="password"
              value={createForm.password}
              onChange={(e) => setCreateForm({ ...createForm, password: e.target.value })}
              required
              fullWidth
              helperText="Minimum 8 characters"
            />
            <FormControl fullWidth>
              <InputLabel>Role</InputLabel>
              <Select
                value={createForm.role}
                label="Role"
                onChange={(e) => setCreateForm({ ...createForm, role: e.target.value })}
              >
                {ROLES.map((r) => (
                  <MenuItem key={r} value={r}>{r}</MenuItem>
                ))}
              </Select>
            </FormControl>
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => { setCreateOpen(false); setCreateError(null); }} disabled={createLoading}>
            Cancel
          </Button>
          <Button
            variant="contained"
            onClick={handleCreateUser}
            disabled={createLoading}
          >
            {createLoading ? <CircularProgress size={20} color="inherit" /> : 'Create User'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Deactivate Confirm Dialog */}
      <Dialog open={!!deactivateTarget} onClose={() => setDeactivateTarget(null)} maxWidth="xs" fullWidth>
        <DialogTitle>Deactivate User</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to deactivate <strong>{deactivateTarget?.username}</strong>?
            They will no longer be able to log in.
          </Typography>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setDeactivateTarget(null)} disabled={deactivateLoading}>
            Cancel
          </Button>
          <Button
            variant="contained"
            color="warning"
            onClick={handleDeactivate}
            disabled={deactivateLoading}
          >
            {deactivateLoading ? <CircularProgress size={20} color="inherit" /> : 'Deactivate'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default UserManagementPage;
