import React, { useState } from 'react';
import { Box, Grid, Typography } from '@mui/material';
import ChatPanel from '../components/agent/ChatPanel';
import AgentInsightFeed from '../components/agent/AgentInsightFeed';

const AssistantPage: React.FC = () => {
  const [chatSeed, setChatSeed] = useState<string | null>(null);
  const [activeInsightCount, setActiveInsightCount] = useState(0);

  return (
    <Box>
      <Typography variant="h4" sx={{ fontWeight: 700, mb: 1 }}>
        Assistant
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Chat on demand, plus automatic insights when alerts, device health, or MQTT status change.
        {activeInsightCount > 0 ? ` ${activeInsightCount} active insight(s).` : ''}
      </Typography>

      <Grid container spacing={3}>
        <Grid item xs={12} lg={5}>
          <AgentInsightFeed
            onOpenInChat={(seed) => setChatSeed(seed)}
            onActiveCountChange={setActiveInsightCount}
          />
        </Grid>
        <Grid item xs={12} lg={7}>
          <ChatPanel seedMessage={chatSeed} />
        </Grid>
      </Grid>
    </Box>
  );
};

export default AssistantPage;
