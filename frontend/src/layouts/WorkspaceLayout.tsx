import { useState } from 'react';
import { Box, Button, Chip, Typography } from '@mui/material';
import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../auth/AuthProvider';
import { roleConfig } from '../auth/roleConfig';

export function WorkspaceLayout() {
  const { user, logout } = useAuth();
  const [leaving, setLeaving] = useState(false);
  if (!user) return null;
  const config = roleConfig[user.role];
  async function signOut() {
    setLeaving(true);
    try { await logout(); } catch { /* Local authentication is cleared even if the network is unavailable. */ }
    finally { setLeaving(false); }
  }
  return <div className="workspace">
    <aside className="sidebar" style={{ borderTop: '5px solid ' + config.color }}>
      <div className="brand-mark">F</div>
      <Typography variant="h6" fontWeight={700}>Fleet Operations</Typography>
      <p className="sidebar-caption">{config.label.toUpperCase()}</p>
      <nav aria-label="Main navigation">
        {config.navigation.map(item => item.path
          ? <NavLink key={item.label} to={item.path} end>{item.label}</NavLink>
          : <div key={item.label} className="planned-nav" aria-disabled="true">{item.label}<span>Planned</span></div>)}
      </nav>
      <div className="sidebar-footer">Reliable journeys.<br />Coordinated operations.</div>
    </aside>
    <div className="workspace-main">
      <header className="topbar">
        <Typography fontWeight={600}>{config.label} workspace</Typography>
        <Box className="flex flex-wrap items-center gap-3">
          <Chip label={user.displayName} size="small" variant="outlined" />
          <Button onClick={() => void signOut()} disabled={leaving}>Sign out</Button>
        </Box>
      </header>
      <Box component="main" sx={{ p: { xs: 2, md: 5 }, maxWidth: 1440, mx: 'auto' }}><Outlet /></Box>
    </div>
  </div>;
}
