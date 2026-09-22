import { useEffect, useState } from 'react';
import { Alert, Box, Button, Chip, Paper, Typography } from '@mui/material';
import { checkHealth } from '../api/health';
const modules = [
  ['01', 'Fleet & people', 'Vehicles, drivers, customer companies and controlled user access.'],
  ['02', 'Orders & dispatch', 'Customer orders, resource assignment and a controlled trip lifecycle.'],
  ['03', 'Maintenance', 'Service history, date and odometer schedules, and driver observations.'],
  ['04', 'Insights & reports', 'Role-specific summaries, fleet activity and spreadsheet exports.'],
];
export function OverviewPage() {
  const [status, setStatus] = useState<'loading' | 'online' | 'offline'>('loading');
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    const controller = new AbortController();
    setStatus('loading');
    checkHealth(controller.signal).then(() => setStatus('online')).catch(() => { if (!controller.signal.aborted) setStatus('offline'); });
    return () => controller.abort();
  }, [attempt]);
  return <><Typography variant="overline" color="primary">FLEET MANAGEMENT SYSTEM</Typography><Typography variant="h3" sx={{ fontSize: { xs: 30, md: 42 }, fontWeight: 750, mt: 1 }}>A clear view of every journey.</Typography><Typography color="text.secondary" sx={{ mt: 2, mb: 4, maxWidth: 650 }}>One workspace for fleet records, dispatch, driver workflows and maintenance. The project foundation is ready for incremental implementation.</Typography><Paper variant="outlined" sx={{ p: 3, mb: 4 }}><Box className="flex flex-wrap items-center justify-between gap-4"><div><Typography variant="h6">System connection</Typography><Typography color="text.secondary" sx={{ mt: 1 }}>Live API liveness check</Typography></div><Chip label={status === 'loading' ? 'Checking API…' : status === 'online' ? 'API connected' : 'API unavailable'} color={status === 'online' ? 'success' : 'default'}/></Box>{status === 'offline' && <Alert severity="warning" sx={{ mt: 2 }} action={<Button color="inherit" onClick={() => setAttempt(v => v + 1)}>Retry</Button>}>Start the backend to connect this workspace.</Alert>}</Paper><Typography variant="h6" sx={{ mb: 2 }}>Planned workspace modules</Typography><div className="grid grid-cols-1 gap-5 md:grid-cols-2">{modules.map(([number, title, description]) => <Paper key={number} variant="outlined" sx={{ p: 3 }}><Typography variant="overline" color="primary">MODULE {number}</Typography><Typography variant="h6" sx={{ mt: 1 }}>{title}</Typography><Typography color="text.secondary" sx={{ mt: 1, mb: 2 }}>{description}</Typography><Chip size="small" label="Planned" variant="outlined"/></Paper>)}</div><Typography variant="body2" color="text.secondary" sx={{ mt: 4 }}>No operational data or demo accounts have been created yet. Authentication and business modules arrive in subsequent phases.</Typography></>;
}
