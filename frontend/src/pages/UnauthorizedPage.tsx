import { Button, Paper, Typography } from '@mui/material';
import { Link } from 'react-router-dom';

export function UnauthorizedPage() {
  return <Paper variant="outlined" sx={{ p: 4 }}>
    <Typography variant="overline">403 · ACCESS RESTRICTED</Typography>
    <Typography variant="h4" sx={{ my: 2 }}>This page is not available for your role.</Typography>
    <Typography color="text.secondary" sx={{ mb: 3 }}>Return to your dashboard to see the tools available to you.</Typography>
    <Button component={Link} to="/" variant="contained">Go to dashboard</Button>
  </Paper>;
}
