import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import {
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  IconButton,
  InputAdornment,
  TextField,
  Typography,
  Alert,
  Link,
} from '@mui/material';
import { Visibility, VisibilityOff, Router as RouterIcon } from '@mui/icons-material';
import authService from '../services/authService';

const LINKEDIN_URL = 'https://www.linkedin.com/in/tranrtc';
const GITHUB_URL = 'https://github.com/TranRTC/capstone_project';

const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as any)?.from?.pathname || '/dashboard';

  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const showDevCredentials = process.env.NODE_ENV !== 'production';

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!username.trim() || !password) return;

    setLoading(true);
    setError(null);

    try {
      await authService.login({ username: username.trim(), password });
      navigate(from, { replace: true });
    } catch (err: any) {
      const msg =
        err.response?.data?.message ||
        err.response?.status === 401
          ? 'Invalid username or password.'
          : 'Login failed. Please try again.';
      setError(msg as string);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'linear-gradient(135deg, #1a237e 0%, #283593 50%, #1565c0 100%)',
        py: 4,
      }}
    >
      <Card
        elevation={12}
        sx={{
          width: '100%',
          maxWidth: 420,
          borderRadius: 3,
          overflow: 'visible',
        }}
      >
        <CardContent sx={{ p: 4 }}>
          {/* Logo / Title */}
          <Box sx={{ textAlign: 'center', mb: 3 }}>
            <Box
              sx={{
                width: 64,
                height: 64,
                borderRadius: '50%',
                background: 'linear-gradient(135deg, #1565c0, #0d47a1)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                mx: 'auto',
                mb: 2,
                boxShadow: '0 4px 14px rgba(21,101,192,0.4)',
              }}
            >
              <RouterIcon sx={{ color: 'white', fontSize: 32 }} />
            </Box>
            <Typography variant="h5" fontWeight={700} color="text.primary">
              IoT Dashboard
            </Typography>
            <Typography variant="body2" color="text.secondary" mt={0.5}>
              Sign in to your account
            </Typography>
          </Box>

          {error && (
            <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
              {error}
            </Alert>
          )}

          <Box component="form" onSubmit={handleSubmit} noValidate>
            <TextField
              fullWidth
              label="Username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              margin="normal"
              autoComplete="username"
              autoFocus
              disabled={loading}
              required
            />
            <TextField
              fullWidth
              label="Password"
              type={showPassword ? 'text' : 'password'}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              margin="normal"
              autoComplete="current-password"
              disabled={loading}
              required
              InputProps={{
                endAdornment: (
                  <InputAdornment position="end">
                    <IconButton
                      onClick={() => setShowPassword(!showPassword)}
                      edge="end"
                      tabIndex={-1}
                    >
                      {showPassword ? <VisibilityOff /> : <Visibility />}
                    </IconButton>
                  </InputAdornment>
                ),
              }}
            />

            <Button
              type="submit"
              fullWidth
              variant="contained"
              size="large"
              disabled={loading || !username.trim() || !password}
              sx={{ mt: 3, mb: 1, borderRadius: 2, py: 1.4, fontWeight: 600 }}
            >
              {loading ? <CircularProgress size={24} color="inherit" /> : 'Sign In'}
            </Button>
          </Box>

          {showDevCredentials && (
            <Typography variant="caption" color="text.secondary" display="block" textAlign="center" mt={2}>
              Default admin: <strong>admin</strong> / <strong>Admin@123</strong>
            </Typography>
          )}
        </CardContent>
      </Card>

      <Box sx={{ mt: 3, maxWidth: 480, px: 2, textAlign: 'center', color: 'rgba(255,255,255,0.88)' }}>
        <Typography variant="caption" display="block" sx={{ lineHeight: 1.6, color: 'inherit' }}>
          BAS Application Development — Capstone Project
        </Typography>
        <Typography variant="caption" display="block" sx={{ mt: 0.75, lineHeight: 1.6, color: 'inherit' }}>
          Developed By Quoc Bao Tran ·{' '}
          <Link
            href={LINKEDIN_URL}
            target="_blank"
            rel="noopener noreferrer"
            sx={{ color: 'inherit', fontWeight: 600 }}
            underline="hover"
          >
            LinkedIn
          </Link>
          {' · '}
          <Link
            href={GITHUB_URL}
            target="_blank"
            rel="noopener noreferrer"
            sx={{ color: 'inherit', fontWeight: 600 }}
            underline="hover"
          >
            GitHub
          </Link>
        </Typography>
      </Box>
    </Box>
  );
};

export default LoginPage;
