import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { Box, CircularProgress } from '@mui/material';
import { useAuth } from './AuthProvider';
import type { Role } from './types';

export function AuthLoading() {
  return <Box role="status" aria-label="Checking session" sx={{ p: 6, textAlign: 'center' }}>
    <CircularProgress aria-label="Loading authentication" />
  </Box>;
}
export function ProtectedRoute({ roles }: { roles?: readonly Role[] }) {
  const { user, status } = useAuth();
  const location = useLocation();
  if (status === 'loading') return <AuthLoading />;
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  if (roles && !roles.includes(user.role)) return <Navigate to="/forbidden" replace />;
  return <Outlet />;
}
