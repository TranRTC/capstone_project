import React, { useEffect, useState } from 'react';
import { Alert, Box, Chip, Paper, Stack, Typography } from '@mui/material';
import { AgentAuditLogEntry, AgentMetrics, getAgentAuditLog, getAgentMetrics } from '../../services/agentService';

const AgentAdminPanel: React.FC = () => {
  const [metrics, setMetrics] = useState<AgentMetrics | null>(null);
  const [audit, setAudit] = useState<AgentAuditLogEntry[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void (async () => {
      try {
        const [m, a] = await Promise.all([getAgentMetrics(), getAgentAuditLog(20)]);
        setMetrics(m);
        setAudit(a);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Could not load assistant admin data.');
      }
    })();
  }, []);

  if (error) {
    return (
      <Alert severity="info" sx={{ mb: 2 }}>
        {error}
      </Alert>
    );
  }

  if (!metrics) return null;

  return (
    <Paper sx={{ p: 2, mb: 3, border: 1, borderColor: 'divider', boxShadow: 'none' }}>
      <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 1 }}>
        Assistant operations (24h)
      </Typography>
      <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1, mb: 2 }}>
        <Chip label={`Chats: ${metrics.chatRequestsLast24h}`} size="small" />
        <Chip label={`Tool calls: ${metrics.toolCallsLast24h}`} size="small" />
        <Chip label={`Actions confirmed: ${metrics.actionsConfirmedLast24h}`} size="small" />
        <Chip label={`Avg ${Math.round(metrics.averageChatDurationMs)}ms`} size="small" />
        {metrics.loopLimitHitsLast24h > 0 && (
          <Chip label={`Loop limits: ${metrics.loopLimitHitsLast24h}`} size="small" color="warning" />
        )}
      </Stack>
      {audit.length > 0 && (
        <Box>
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
            Recent audit
          </Typography>
          {audit.slice(0, 5).map((entry) => (
            <Typography key={entry.agentAuditLogId} variant="caption" sx={{ display: 'block' }}>
              {new Date(entry.createdAt).toLocaleString()} · {entry.eventType}
              {entry.toolName ? ` · ${entry.toolName}` : ''} · {entry.username}
            </Typography>
          ))}
        </Box>
      )}
    </Paper>
  );
};

export default AgentAdminPanel;
