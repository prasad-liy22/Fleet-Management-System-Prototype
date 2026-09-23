import { useState, type FormEvent } from 'react';
import { Alert, Box, Button, Link, Paper, Stack, TextField, Typography } from '@mui/material';
import { Link as RouterLink, Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthProvider';
import { AuthLoading } from '../auth/ProtectedRoute';

export function LoginPage() {
  const auth = useAuth();
  const location = useLocation();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  if (auth.status === 'loading') return <AuthLoading />;
  const destination = (location.state as { from?: string } | null)?.from;
  if (auth.user) return <Navigate to={destination?.startsWith('/') && !destination.startsWith('//') ? destination : '/'} replace />;
  async function submit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError('');
    try { await auth.login(email, password); }
    catch (cause) { setError(cause instanceof Error ? cause.message : 'Unable to sign in.'); }
    finally { setBusy(false); }
  }
  return <Box className="auth-screen"><Paper className="auth-card" elevation={0}>
    <Typography color="primary" variant="overline">FLEET OPERATIONS</Typography>
    <Typography variant="h4" fontWeight={700} sx={{ mt: 1 }}>Welcome back</Typography>
    <Typography color="text.secondary" sx={{ my: 2 }}>Sign in to your fleet workspace.</Typography>
    {auth.message && <Alert severity="info" sx={{ mb: 2 }}>{auth.message}</Alert>}
    {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
    <Stack component="form" onSubmit={submit} spacing={2}>
      <TextField label="Email" type="email" autoComplete="username" value={email} onChange={e => setEmail(e.target.value)} required fullWidth />
      <TextField label="Password" type="password" autoComplete="current-password" value={password} onChange={e => setPassword(e.target.value)} required fullWidth />
      <Button type="submit" variant="contained" size="large" disabled={busy}>{busy ? 'Signing in…' : 'Sign in'}</Button>
      <Link component={RouterLink} to="/forgot-password">Forgot your password?</Link>
    </Stack>
  </Paper></Box>;
}
