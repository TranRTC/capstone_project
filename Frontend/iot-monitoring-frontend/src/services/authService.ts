import axios from 'axios';
import { runtimeConfig } from '../config/runtimeConfig';

const AUTH_BASE_URL = runtimeConfig.apiBaseUrl;

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  username: string;
  role: string;
  expiresAt: string;
}

export interface UserInfo {
  userId: number;
  username: string;
  email?: string;
  role: string;
  isActive: boolean;
  createdAt: string;
}

const TOKEN_KEY = 'iot_jwt_token';
const USER_KEY = 'iot_user';

const authApi = axios.create({
  baseURL: AUTH_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

// Attach JWT to every request automatically
authApi.interceptors.request.use((config) => {
  const token = getToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export const authService = {
  async login(credentials: LoginRequest): Promise<LoginResponse> {
    const response = await authApi.post<LoginResponse>('/auth/login', credentials);
    const data = response.data;
    localStorage.setItem(TOKEN_KEY, data.token);
    localStorage.setItem(USER_KEY, JSON.stringify({ username: data.username, role: data.role, expiresAt: data.expiresAt }));
    return data;
  },

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  },

  getToken(): string | null {
    return getToken();
  },

  getCurrentUser(): { username: string; role: string; expiresAt: string } | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw);
    } catch {
      return null;
    }
  },

  isAuthenticated(): boolean {
    const token = getToken();
    if (!token) return false;
    // Check expiry stored in localStorage (quick check without decoding JWT)
    const user = this.getCurrentUser();
    if (user?.expiresAt) {
      const expiry = new Date(user.expiresAt);
      if (expiry <= new Date()) {
        this.logout();
        return false;
      }
    }
    return true;
  },

  isAdmin(): boolean {
    return this.getCurrentUser()?.role === 'Admin';
  },
};

function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export default authService;
