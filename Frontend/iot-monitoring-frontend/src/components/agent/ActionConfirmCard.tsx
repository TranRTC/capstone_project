import React from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  Paper,
  Stack,
  Typography,
} from '@mui/material';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import CancelIcon from '@mui/icons-material/Cancel';
import PendingActionsIcon from '@mui/icons-material/PendingActions';
import { AgentActionProposal } from '../../services/agentService';

interface ActionConfirmCardProps {
  proposal: AgentActionProposal;
  onConfirm: (id: number) => void;
  onCancel: (id: number) => void;
  loading?: boolean;
}

function actionLabel(actionType: string): string {
  switch (actionType) {
    case 'AcknowledgeAlert':
      return 'Acknowledge Alert';
    case 'ResolveAlert':
      return 'Resolve Alert';
    case 'CreateDevice':
      return 'Create Device';
    case 'SendDeviceCommand':
      return 'Send Command';
    default:
      return actionType;
  }
}

const ActionConfirmCard: React.FC<ActionConfirmCardProps> = ({
  proposal,
  onConfirm,
  onCancel,
  loading = false,
}) => {
  const expiresAt = new Date(proposal.expiresAt);
  const isExpired = expiresAt <= new Date();

  return (
    <Paper
      variant="outlined"
      sx={{
        p: 2,
        borderColor: 'warning.light',
        bgcolor: 'warning.50',
      }}
    >
      <Stack spacing={1.5}>
        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
          <PendingActionsIcon color="warning" fontSize="small" />
          <Typography variant="subtitle2" sx={{ fontWeight: 700, flex: 1 }}>
            Pending action — confirm to execute
          </Typography>
          <Chip size="small" label={actionLabel(proposal.actionType)} color="warning" />
        </Stack>

        <Typography variant="body2">{proposal.summary}</Typography>

        {isExpired ? (
          <Alert severity="warning" sx={{ py: 0.5 }}>
            This proposal has expired. Ask the assistant to propose it again.
          </Alert>
        ) : (
          <Typography variant="caption" color="text.secondary">
            Expires {expiresAt.toLocaleString()}
          </Typography>
        )}

        {proposal.canConfirm && !isExpired && (
          <Stack direction="row" spacing={1} justifyContent="flex-end">
            <Button
              size="small"
              color="inherit"
              startIcon={<CancelIcon />}
              onClick={() => onCancel(proposal.agentActionProposalId)}
              disabled={loading}
            >
              Cancel
            </Button>
            <Button
              size="small"
              variant="contained"
              color="warning"
              startIcon={<CheckCircleIcon />}
              onClick={() => onConfirm(proposal.agentActionProposalId)}
              disabled={loading}
            >
              Confirm
            </Button>
          </Stack>
        )}

        {!proposal.canConfirm && !isExpired && (
          <Box>
            <Typography variant="caption" color="text.secondary">
              Your role is read-only. An Admin or Operator must confirm this action.
            </Typography>
          </Box>
        )}
      </Stack>
    </Paper>
  );
};

export default ActionConfirmCard;
