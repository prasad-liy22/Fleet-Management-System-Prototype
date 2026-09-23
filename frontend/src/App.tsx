import { lazy, Suspense } from 'react';
import { Route, Routes } from 'react-router-dom';
import { Typography } from '@mui/material';
import { AuthProvider } from './auth/AuthProvider';
import { ProtectedRoute } from './auth/ProtectedRoute';
import { WorkspaceLayout } from './layouts/WorkspaceLayout';
import { LoginPage } from './pages/LoginPage';
import { ForgotPasswordPage, ResetPasswordPage } from './pages/PasswordPages';
import { RoleHomePage } from './pages/RoleHomePage';
import { UnauthorizedPage } from './pages/UnauthorizedPage';
const UsersPage = lazy(() => import('./features/users/UsersPage').then(module => ({ default: module.UsersPage })));

export default function App() {
  return <AuthProvider><Routes>
    <Route path="/login" element={<LoginPage />} />
    <Route path="/forgot-password" element={<ForgotPasswordPage />} />
    <Route path="/reset-password" element={<ResetPasswordPage />} />
    <Route element={<ProtectedRoute />}>
      <Route element={<WorkspaceLayout />}>
        <Route index element={<RoleHomePage />} />
        <Route path="/forbidden" element={<UnauthorizedPage />} />
        <Route element={<ProtectedRoute roles={['FleetAdministrator']} />}>
          <Route path="/users" element={<Suspense fallback={<Typography>Loading users…</Typography>}><UsersPage /></Suspense>} />
        </Route>
        <Route path="*" element={<Typography variant="h4">Page not found</Typography>} />
      </Route>
    </Route>
  </Routes></AuthProvider>;
}
