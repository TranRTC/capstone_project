import React, { useCallback, useEffect, useState } from 'react';
import {
  Alert,
  Box,
  CircularProgress,
  Snackbar,
  Stack,
  Typography,
} from '@mui/material';
import InsightsIcon from '@mui/icons-material/Insights';
import {
  AgentInsight,
  dismissAgentInsight,
  getAgentInsights,
  getOpenInChatSeed,
} from '../../services/agentService';
import { signalRService } from '../../services/signalRService';
import AgentInsightCard from './AgentInsightCard';

interface AgentInsightFeedProps {
  onOpenInChat: (seedMessage: string) => void;
  onActiveCountChange?: (count: number) => void;
}

const AgentInsightFeed: React.FC<AgentInsightFeedProps> = ({
  onOpenInChat,
  onActiveCountChange,
}) => {
  const [insights, setInsights] = useState<AgentInsight[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dismissingId, setDismissingId] = useState<number | null>(null);
  const [toastInsight, setToastInsight] = useState<AgentInsight | null>(null);

  const loadInsights = useCallback(async () => {
    try {
      setError(null);
      const result = await getAgentInsights('Active', 1, 20);
      setInsights(result.items);
      onActiveCountChange?.(result.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not load insights.');
    } finally {
      setLoading(false);
    }
  }, [onActiveCountChange]);

  useEffect(() => {
    void loadInsights();
  }, [loadInsights]);

  useEffect(() => {
    const handleInsight = (insight: AgentInsight) => {
      setInsights((prev) => {
        const exists = prev.some((item) => item.agentInsightId === insight.agentInsightId);
        const next = exists
          ? prev.map((item) => (item.agentInsightId === insight.agentInsightId ? insight : item))
          : [insight, ...prev];
        onActiveCountChange?.(next.length);
        return next;
      });
      if (insight.severity === 'critical' || insight.severity === 'warning') {
        setToastInsight(insight);
      }
    };

    void (async () => {
      try {
        await signalRService.start();
        await signalRService.subscribeToAgentInsights();
        signalRService.onAgentInsightCreated(handleInsight);
      } catch {
        // Feed still works via REST polling on page load.
      }
    })();

    return () => {
      signalRService.offAgentInsightCreated(handleInsight);
    };
  }, [onActiveCountChange]);

  const handleDismiss = async (id: number) => {
    setDismissingId(id);
    try {
      await dismissAgentInsight(id);
      setInsights((prev) => {
        const next = prev.filter((item) => item.agentInsightId !== id);
        onActiveCountChange?.(next.length);
        return next;
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not dismiss insight.');
    } finally {
      setDismissingId(null);
    }
  };

  const handleOpenInChat = async (insight: AgentInsight) => {
    try {
      const seed = insight.chatSeedMessage ?? (await getOpenInChatSeed(insight.agentInsightId));
      onOpenInChat(seed);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not open insight in chat.');
    }
  };

  return (
    <Box sx={{ mb: 3 }}>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1.5 }}>
        <InsightsIcon color="primary" />
        <Typography variant="h6" sx={{ fontWeight: 700 }}>
          Proactive Insights
        </Typography>
      </Stack>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Automatic monitoring of alerts, offline devices, and MQTT health.
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {loading ? (
        <Stack direction="row" spacing={1} alignItems="center">
          <CircularProgress size={18} />
          <Typography variant="body2" color="text.secondary">
            Loading insights...
          </Typography>
        </Stack>
      ) : insights.length === 0 ? (
        <Alert severity="info">No active insights right now. The assistant will post here when something needs attention.</Alert>
      ) : (
        <Stack spacing={1.5}>
          {insights.map((insight) => (
            <AgentInsightCard
              key={insight.agentInsightId}
              insight={insight}
              onDismiss={handleDismiss}
              onOpenInChat={handleOpenInChat}
              dismissing={dismissingId === insight.agentInsightId}
            />
          ))}
        </Stack>
      )}

      <Snackbar
        open={toastInsight !== null}
        autoHideDuration={8000}
        onClose={() => setToastInsight(null)}
        anchorOrigin={{ vertical: 'top', horizontal: 'right' }}
      >
        <Alert
          severity={toastInsight?.severity === 'critical' ? 'error' : 'warning'}
          onClose={() => setToastInsight(null)}
          sx={{ width: '100%' }}
        >
          {toastInsight?.title}
        </Alert>
      </Snackbar>
    </Box>
  );
};

export default AgentInsightFeed;
