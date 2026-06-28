import axios, { isAxiosError } from 'axios';
import { ApiResponse } from '../types';
import { runtimeConfig } from '../config/runtimeConfig';

export interface AgentChatMessage {
  role: 'user' | 'assistant';
  content: string;
}

export interface AgentChatContext {
  deviceId?: number;
  alertId?: number;
  route?: string;
}

export interface AgentChatResponse {
  reply: string;
  toolsUsed: string[];
  docSourcesUsed?: string[];
  pendingAction?: AgentActionProposal | null;
  sessionId?: number;
  dataAsOfUtc?: string;
  contextUsed?: AgentChatContext;
  usedIntentRouter?: boolean;
}

export interface AgentActionProposal {
  agentActionProposalId: number;
  actionType: string;
  summary: string;
  status: string;
  relatedAlertId?: number;
  relatedDeviceId?: number;
  createdAt: string;
  expiresAt: string;
  canConfirm: boolean;
}

export interface AgentActionResult {
  agentActionProposalId: number;
  actionType: string;
  status: string;
  message: string;
  resultJson?: string;
}

export interface AgentStatus {
  enabled: boolean;
  configured: boolean;
  model: string;
  setupHint?: string;
}

export interface AgentInsight {
  agentInsightId: number;
  triggerType: string;
  severity: string;
  title: string;
  summary: string;
  suggestedActions: string[];
  relatedAlertId?: number;
  relatedDeviceId?: number;
  status: string;
  usedLlm: boolean;
  createdAt: string;
  dismissedAt?: string;
  chatSeedMessage?: string;
}

export interface AgentProactiveStatus {
  enabled: boolean;
  llmConfigured: boolean;
  activeInsightCount: number;
  lastSweepAtUtc?: string;
}

export interface AgentMetrics {
  chatRequestsLast24h: number;
  toolCallsLast24h: number;
  actionsConfirmedLast24h: number;
  loopLimitHitsLast24h: number;
  llmErrorsLast24h: number;
  averageChatDurationMs: number;
  topTools: { toolName: string; count: number }[];
  generatedAtUtc: string;
}

export interface AgentAuditLogEntry {
  agentAuditLogId: number;
  eventType: string;
  username: string;
  userRole?: string;
  toolName?: string;
  summary?: string;
  relatedDeviceId?: number;
  durationMs?: number;
  success: boolean;
  createdAt: string;
}

export interface PagedAgentInsights {
  items: AgentInsight[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

const agentApi = axios.create({
  baseURL: runtimeConfig.apiBaseUrl,
  headers: { 'Content-Type': 'application/json' },
});

agentApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('iot_jwt_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

function extractErrorMessage(error: unknown): string {
  if (isAxiosError(error)) {
    const apiMessage = error.response?.data?.message;
    if (typeof apiMessage === 'string' && apiMessage.trim()) {
      return apiMessage;
    }
    if (error.response?.status === 404) {
      return 'Assistant endpoint not found. Restart the backend API to load the latest code.';
    }
    if (error.code === 'ERR_NETWORK') {
      return 'Cannot reach the API. Ensure the backend is running on http://localhost:5000.';
    }
  }
  if (error instanceof Error && error.message) {
    return error.message;
  }
  return 'Assistant request failed.';
}

export async function getAgentStatus(): Promise<AgentStatus> {
  const response = await agentApi.get<ApiResponse<AgentStatus>>('/agent/status');
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message || 'Could not load assistant status.');
  }
  return response.data.data;
}

export async function sendAgentMessage(
  message: string,
  history?: AgentChatMessage[],
  options?: { sessionId?: number; context?: AgentChatContext }
): Promise<AgentChatResponse> {
  try {
    const response = await agentApi.post<ApiResponse<AgentChatResponse>>('/agent/chat', {
      message,
      history,
      sessionId: options?.sessionId,
      context: options?.context,
    });

    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Assistant request failed.');
    }

    return response.data.data;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

export async function getAgentInsights(
  status = 'Active',
  pageNumber = 1,
  pageSize = 20
): Promise<PagedAgentInsights> {
  try {
    const response = await agentApi.get<ApiResponse<PagedAgentInsights>>('/agent/insights', {
      params: { status, pageNumber, pageSize },
    });
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Could not load insights.');
    }
    return response.data.data;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

export async function getProactiveStatus(): Promise<AgentProactiveStatus> {
  const response = await agentApi.get<ApiResponse<AgentProactiveStatus>>('/agent/proactive/status');
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message || 'Could not load proactive status.');
  }
  return response.data.data;
}

export async function dismissAgentInsight(id: number): Promise<AgentInsight> {
  const response = await agentApi.post<ApiResponse<AgentInsight>>(`/agent/insights/${id}/dismiss`);
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message || 'Could not dismiss insight.');
  }
  return response.data.data;
}

export async function getOpenInChatSeed(id: number): Promise<string> {
  const response = await agentApi.get<ApiResponse<{ seedMessage: string }>>(
    `/agent/insights/${id}/open-in-chat`
  );
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message || 'Could not open insight in chat.');
  }
  return response.data.data.seedMessage;
}

export async function getPendingAgentAction(): Promise<AgentActionProposal | null> {
  try {
    const response = await agentApi.get<ApiResponse<AgentActionProposal | null>>('/agent/actions/pending');
    if (!response.data.success) {
      throw new Error(response.data.message || 'Could not load pending action.');
    }
    return response.data.data ?? null;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

export async function confirmAgentAction(id: number): Promise<AgentActionResult> {
  try {
    const response = await agentApi.post<ApiResponse<AgentActionResult>>(`/agent/actions/${id}/confirm`);
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Could not confirm action.');
    }
    return response.data.data;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

export async function cancelAgentAction(id: number): Promise<AgentActionProposal> {
  try {
    const response = await agentApi.post<ApiResponse<AgentActionProposal>>(`/agent/actions/${id}/cancel`);
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Could not cancel action.');
    }
    return response.data.data;
  } catch (error) {
    throw new Error(extractErrorMessage(error));
  }
}

export async function getSuggestedPrompts(deviceId?: number): Promise<string[]> {
  const response = await agentApi.get<ApiResponse<{ prompts: string[] }>>('/agent/suggested-prompts', {
    params: deviceId ? { deviceId } : undefined,
  });
  if (!response.data.success || !response.data.data) {
    return [];
  }
  return response.data.data.prompts;
}

export async function getAgentMetrics(): Promise<AgentMetrics> {
  const response = await agentApi.get<ApiResponse<AgentMetrics>>('/agent/metrics');
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message || 'Could not load metrics.');
  }
  return response.data.data;
}

export async function getAgentAuditLog(take = 50): Promise<AgentAuditLogEntry[]> {
  const response = await agentApi.get<ApiResponse<AgentAuditLogEntry[]>>('/agent/audit', { params: { take } });
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message || 'Could not load audit log.');
  }
  return response.data.data;
}
