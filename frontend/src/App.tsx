import { Route, Routes } from 'react-router-dom';
import { WorkspaceLayout } from './layouts/WorkspaceLayout';
import { OverviewPage } from './pages/OverviewPage';
import { Typography } from '@mui/material';
export default function App() {
  return <WorkspaceLayout><Routes><Route path="/" element={<OverviewPage/>}/><Route path="*" element={<Typography variant="h4">Page not found</Typography>}/></Routes></WorkspaceLayout>;
}
