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

const VehiclesPage = lazy(() => import('./features/vehicles/VehiclesPage').then(m => ({ default: m.VehiclesPage })));
const DriversPage = lazy(() => import('./features/drivers/DriversPage').then(m => ({ default: m.DriversPage })));
const CustomersPage = lazy(() => import('./features/customers/CustomersPage').then(m => ({ default: m.CustomersPage })));

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
          <Route path="/vehicles" element={<Suspense fallback={<Typography>Loading vehicles…</Typography>}><VehiclesPage /></Suspense>} />
          <Route path="/drivers" element={<Suspense fallback={<Typography>Loading drivers…</Typography>}><DriversPage /></Suspense>} />
          <Route path="/customers" element={<Suspense fallback={<Typography>Loading customers…</Typography>}><CustomersPage /></Suspense>} />
          <Route path="/users" element={<Suspense fallback={<Typography>Loading users…</Typography>}><UsersPage /></Suspense>} />
        </Route>
        <Route path="*" element={<Typography variant="h4">Page not found</Typography>} />
      </Route>
    </Route>
  </Routes></AuthProvider>;
}
