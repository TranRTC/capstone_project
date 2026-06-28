import React, { useState } from 'react';
import { Box, Button, Grid, Typography } from '@mui/material';
import { Link as RouterLink, useSearchParams } from 'react-router-dom';
import ChatPanel from '../components/agent/ChatPanel';
import AgentInsightFeed from '../components/agent/AgentInsightFeed';
import AgentAdminPanel from '../components/agent/AgentAdminPanel';
import { authService } from '../services/authService';

const AssistantPage: React.FC = () => {
  const [chatSeed, setChatSeed] = useState<string | null>(null);
  const [activeInsightCount, setActiveInsightCount] = useState(0);
  const [searchParams] = useSearchParams();
  const deviceIdParam = searchParams.get('deviceId');
  const deviceId = deviceIdParam ? Number(deviceIdParam) : undefined;
  const isAdmin = authService.isAdmin();

  const context = deviceId && !Number.isNaN(deviceId)
    ? { deviceId, route: `/assistant?deviceId=${deviceId}` }
    : { route: '/assistant' };

  return (
    <Box>
      <Typography variant="h4" sx={{ fontWeight: 700, mb: 1 }}>
        Assistant
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Professional IoT assistant with live data, audit trail, and context-aware answers.
        {activeInsightCount > 0 ? ` ${activeInsightCount} active insight(s).` : ''}
      </Typography>

      {deviceId && !Number.isNaN(deviceId) && (
        <Button component={RouterLink} to={`/devices/${deviceId}`} size="small" sx={{ mb: 2 }}>
          Back to device {deviceId}
        </Button>
      )}

      {isAdmin && <AgentAdminPanel />}

      <Grid container spacing={3}>
        <Grid item xs={12} lg={5}>
          <AgentInsightFeed
            onOpenInChat={(seed) => setChatSeed(seed)}
            onActiveCountChange={setActiveInsightCount}
          />
        </Grid>
        <Grid item xs={12} lg={7}>
          <ChatPanel seedMessage={chatSeed} context={context} />
        </Grid>
      </Grid>
    </Box>
  );
};

export default AssistantPage;
