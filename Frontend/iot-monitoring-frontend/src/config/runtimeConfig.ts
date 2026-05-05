const fallbackApiBaseUrl = 'http://localhost:5000/api/v1';
const fallbackSignalRHubUrl = 'http://localhost:5000/monitoringhub';

const trimTrailingSlash = (value: string) => value.replace(/\/+$/, '');

const envApiBaseUrl = process.env.REACT_APP_API_BASE_URL?.trim();
const envSignalRHubUrl = process.env.REACT_APP_SIGNALR_HUB_URL?.trim();

export const runtimeConfig = {
  apiBaseUrl: trimTrailingSlash(envApiBaseUrl || fallbackApiBaseUrl),
  signalRHubUrl: trimTrailingSlash(envSignalRHubUrl || fallbackSignalRHubUrl),
} as const;

