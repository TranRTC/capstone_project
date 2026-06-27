import React, { useEffect, useRef, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import SendIcon from '@mui/icons-material/Send';
import SmartToyIcon from '@mui/icons-material/SmartToy';
import PersonIcon from '@mui/icons-material/Person';
import { AgentChatMessage, getAgentStatus, sendAgentMessage } from '../../services/agentService';

const panelSx = {
  p: 2,
  border: 1,
  borderColor: 'divider',
  boxShadow: 'none',
  bgcolor: 'background.paper',
  display: 'flex',
  flexDirection: 'column',
  height: '100%',
  minHeight: 480,
};

const ChatPanel: React.FC<{ seedMessage?: string | null }> = ({ seedMessage = null }) => {
  const [messages, setMessages] = useState<AgentChatMessage[]>([
    {
      role: 'assistant',
      content:
        'Ask about devices, active alerts, recent sensor readings, or system health. I use live data from your monitoring database.',
    },
  ]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [lastToolsUsed, setLastToolsUsed] = useState<string[]>([]);
  const [setupHint, setSetupHint] = useState<string | null>(null);
  const [configured, setConfigured] = useState<boolean | null>(null);
  const scrollRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (seedMessage) {
      setInput(seedMessage);
    }
  }, [seedMessage]);

  useEffect(() => {
    void (async () => {
      try {
        const status = await getAgentStatus();
        setConfigured(status.configured);
        if (!status.configured && status.setupHint) {
          setSetupHint(status.setupHint);
        }
      } catch {
        setConfigured(false);
        setSetupHint('Could not reach the assistant status endpoint. Restart the backend API, then try again.');
      }
    })();
  }, []);

  const handleSend = async () => {
    const trimmed = input.trim();
    if (!trimmed || loading) return;

    const userMessage: AgentChatMessage = { role: 'user', content: trimmed };
    const history = messages.filter((m) => m.role === 'user' || m.role === 'assistant');
    const nextMessages = [...messages, userMessage];

    setMessages(nextMessages);
    setInput('');
    setLoading(true);
    setError(null);

    try {
      const result = await sendAgentMessage(trimmed, history);
      setLastToolsUsed(result.toolsUsed ?? []);
      setMessages((prev) => [...prev, { role: 'assistant', content: result.reply }]);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Assistant request failed.';
      setError(message);
      setMessages((prev) => [
        ...prev,
        {
          role: 'assistant',
          content: message,
        },
      ]);
    } finally {
      setLoading(false);
      requestAnimationFrame(() => {
        scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: 'smooth' });
      });
    }
  };

  const handleKeyDown = (event: React.KeyboardEvent) => {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      void handleSend();
    }
  };

  return (
    <Paper sx={panelSx}>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
        <SmartToyIcon color="primary" />
        <Typography variant="h6" sx={{ fontWeight: 700 }}>
          IoT Assistant
        </Typography>
        {lastToolsUsed.length > 0 && (
          <Chip size="small" label={`Tools: ${lastToolsUsed.join(', ')}`} variant="outlined" />
        )}
      </Stack>

      {setupHint && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          {setupHint}
        </Alert>
      )}

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      <Box
        ref={scrollRef}
        sx={{
          flex: 1,
          overflowY: 'auto',
          mb: 2,
          pr: 1,
        }}
      >
        <Stack spacing={1.5}>
          {messages.map((message, index) => {
            const isUser = message.role === 'user';
            return (
              <Box
                key={`${message.role}-${index}`}
                sx={{
                  display: 'flex',
                  justifyContent: isUser ? 'flex-end' : 'flex-start',
                }}
              >
                <Box
                  sx={{
                    maxWidth: '85%',
                    p: 1.5,
                    borderRadius: 2,
                    bgcolor: isUser ? 'primary.main' : 'action.hover',
                    color: isUser ? 'primary.contrastText' : 'text.primary',
                  }}
                >
                  <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
                    {isUser ? <PersonIcon fontSize="small" /> : <SmartToyIcon fontSize="small" />}
                    <Typography variant="caption" sx={{ fontWeight: 700 }}>
                      {isUser ? 'You' : 'Assistant'}
                    </Typography>
                  </Stack>
                  <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
                    {message.content}
                  </Typography>
                </Box>
              </Box>
            );
          })}
          {loading && (
            <Stack direction="row" spacing={1} alignItems="center">
              <CircularProgress size={18} />
              <Typography variant="body2" color="text.secondary">
                Thinking...
              </Typography>
            </Stack>
          )}
        </Stack>
      </Box>

      <Stack direction="row" spacing={1}>
        <TextField
          fullWidth
          multiline
          maxRows={4}
          placeholder="Ask about devices, alerts, readings, or system health..."
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          disabled={loading || configured === false}
        />
        <Button
          variant="contained"
          endIcon={loading ? <CircularProgress size={16} color="inherit" /> : <SendIcon />}
          onClick={() => void handleSend()}
          disabled={loading || !input.trim() || configured === false}
          sx={{ minWidth: 100, alignSelf: 'flex-end' }}
        >
          Send
        </Button>
      </Stack>
    </Paper>
  );
};

export default ChatPanel;
