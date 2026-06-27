import React from 'react';
import {
  Box,
  Button,
  Chip,
  Paper,
  Stack,
  Typography,
} from '@mui/material';
import type { ChipProps } from '@mui/material/Chip';
import SmartToyIcon from '@mui/icons-material/SmartToy';
import CloseIcon from '@mui/icons-material/Close';
import ChatIcon from '@mui/icons-material/Chat';
import { AgentInsight } from '../../services/agentService';

function severityColor(severity: string): ChipProps['color'] {
  switch (severity.toLowerCase()) {
    case 'critical':
      return 'error';
    case 'warning':
      return 'warning';
    case 'info':
      return 'info';
    default:
      return 'default';
  }
}

interface AgentInsightCardProps {
  insight: AgentInsight;
  onDismiss: (id: number) => void;
  onOpenInChat: (insight: AgentInsight) => void;
  dismissing?: boolean;
}

const AgentInsightCard: React.FC<AgentInsightCardProps> = ({
  insight,
  onDismiss,
  onOpenInChat,
  dismissing = false,
}) => (
  <Paper
    variant="outlined"
    sx={{
      p: 2,
      borderColor: insight.severity === 'critical' ? 'error.light' : 'divider',
      bgcolor: insight.severity === 'critical' ? 'error.50' : 'background.paper',
    }}
  >
    <Stack spacing={1.25}>
      <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
        <SmartToyIcon color="primary" fontSize="small" />
        <Typography variant="subtitle2" sx={{ fontWeight: 700, flex: 1 }}>
          {insight.title}
        </Typography>
        <Chip size="small" label={insight.triggerType} variant="outlined" />
        <Chip size="small" label={insight.severity} color={severityColor(insight.severity)} />
      </Stack>

      <Typography variant="body2" color="text.secondary">
        {insight.summary}
      </Typography>

      {insight.suggestedActions.length > 0 && (
        <Box component="ul" sx={{ m: 0, pl: 2.5 }}>
          {insight.suggestedActions.map((action) => (
            <Typography key={action} component="li" variant="body2" color="text.secondary">
              {action}
            </Typography>
          ))}
        </Box>
      )}

      <Stack direction="row" spacing={1} justifyContent="space-between" alignItems="center">
        <Typography variant="caption" color="text.secondary">
          {new Date(insight.createdAt).toLocaleString()}
          {insight.usedLlm ? ' · AI summary' : ' · Rule-based'}
        </Typography>
        <Stack direction="row" spacing={1}>
          <Button
            size="small"
            startIcon={<ChatIcon />}
            onClick={() => onOpenInChat(insight)}
          >
            Ask follow-up
          </Button>
          <Button
            size="small"
            color="inherit"
            startIcon={<CloseIcon />}
            onClick={() => onDismiss(insight.agentInsightId)}
            disabled={dismissing}
          >
            Dismiss
          </Button>
        </Stack>
      </Stack>
    </Stack>
  </Paper>
);

export default AgentInsightCard;
