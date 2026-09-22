import type { ReactNode } from 'react';
import { Box, Chip, Typography } from '@mui/material';
import { NavLink } from 'react-router-dom';
export function WorkspaceLayout({ children }: { children: ReactNode }) {
  return <div className="workspace"><aside className="sidebar"><div className="brand-mark">F</div><Typography variant="h6" fontWeight={700}>Fleet Operations</Typography><p className="sidebar-caption">UNIVERSITY INDUSTRY PROJECT</p><nav aria-label="Main navigation"><NavLink to="/">Workspace overview</NavLink></nav><div className="sidebar-footer">Reliable journeys.<br/>Coordinated operations.</div></aside><div className="workspace-main"><header className="topbar"><Typography fontWeight={600}>Operations workspace</Typography><Chip label="Foundation phase" size="small" variant="outlined"/></header><Box component="main" sx={{ p: { xs: 2, md: 5 }, maxWidth: 1440, mx: 'auto' }}>{children}</Box></div></div>;
}
