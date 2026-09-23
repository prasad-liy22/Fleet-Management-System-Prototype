import { Box, Button, Chip, Paper, Typography } from '@mui/material';
import { Link } from 'react-router-dom';
import { useAuth } from '../auth/AuthProvider';
import { roleConfig } from '../auth/roleConfig';

export function RoleHomePage() {
  const { user } = useAuth();
  if (!user) return null;
  const config = roleConfig[user.role];
  return <>
    <Paper variant="outlined" sx={{ p: { xs: 3, md: 5 }, borderTop: '6px solid ' + config.color, mb: 3 }}>
      <Chip label={config.label} sx={{ color: config.color, bgcolor: config.color + '12', mb: 3 }} />
      <Typography variant="h3" fontWeight={700} sx={{ fontSize: { xs: 30, md: 42 } }}>Hello, {user.displayName}</Typography>
      <Typography color="text.secondary" sx={{ mt: 2, mb: 3 }}>{config.introduction}</Typography>
      {user.role === 'FleetAdministrator' && <Button component={Link} to="/users" variant="contained">Manage user accounts</Button>}
      {user.role === 'Driver' && <Typography variant="body2">Your account is linked to your driver record. Trip tools will be available in a later phase.</Typography>}
      {user.role === 'FleetOwner' && <Typography variant="body2">Fleet analytics and reports are planned. No operational metrics are shown yet.</Typography>}
    </Paper>
    <Typography variant="h6" sx={{ mb: 2 }}>Your workspace</Typography>
    <Box className="grid grid-cols-1 gap-4 md:grid-cols-2">
      {config.navigation.filter(item => !item.path).map(item => <Paper key={item.label} variant="outlined" sx={{ p: 3 }}>
        <Typography variant="h6">{item.label}</Typography>
        <Typography color="text.secondary" sx={{ mt: 1, mb: 2 }}>This module is scheduled for a later implementation phase.</Typography>
        <Chip label="Planned" size="small" variant="outlined" />
      </Paper>)}
    </Box>
    <Typography variant="body2" color="text.secondary" sx={{ mt: 3 }}>Signed in as {user.email}</Typography>
  </>;
}
