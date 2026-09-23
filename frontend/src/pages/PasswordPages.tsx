import { useEffect, useState, type FormEvent } from 'react';
import { Alert, Box, Button, Link, Paper, Stack, TextField, Typography } from '@mui/material';
import { Link as RouterLink, useNavigate, useSearchParams } from 'react-router-dom';
import { request } from '../api/client';
import { useAuth } from '../auth/AuthProvider';

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('');
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  async function submit(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError(''); setMessage('');
    try {
      const response = await request<{ message: string }>('/auth/forgot-password', {
        method: 'POST', anonymous: true, body: JSON.stringify({ email }),
      });
      setMessage(response.message);
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'Unable to request a reset.'); }
    finally { setBusy(false); }
  }
  return <Box className="auth-screen"><Paper className="auth-card">
    <Typography variant="h4">Reset your password</Typography>
    <Typography color="text.secondary" sx={{ my: 2 }}>Enter your account email to request instructions.</Typography>
    {message && <Alert severity="success" sx={{ mb: 2 }}>{message}</Alert>}
    {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
    <Stack component="form" onSubmit={submit} spacing={2}>
      <TextField label="Email" type="email" autoComplete="email" value={email} onChange={e => setEmail(e.target.value)} required />
      <Button type="submit" variant="contained" disabled={busy}>Request reset</Button>
      <Link component={RouterLink} to="/login">Back to sign in</Link>
    </Stack>
  </Paper></Box>;
}

export function ResetPasswordPage() {
  const auth = useAuth();
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const [link] = useState(() => ({ email: params.get('email') ?? '', token: params.get('token') ?? '' }));
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  useEffect(() => { if (params.has('token')) navigate('/reset-password', { replace: true }); }, [navigate, params]);
  async function submit(event: FormEvent) {
    event.preventDefault();
    if (password !== confirm) { setError('Passwords must match.'); return; }
    setBusy(true); setError('');
    try {
      const response = await request<{ message: string }>('/auth/reset-password', {
        method: 'POST', anonymous: true, body: JSON.stringify({ ...link, password }),
      });
      if (auth.user) await auth.logout().catch(() => {});
      setPassword(''); setConfirm(''); setMessage(response.message);
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'Unable to reset password.'); }
    finally { setBusy(false); }
  }
  return <Box className="auth-screen"><Paper className="auth-card">
    <Typography variant="h4">Choose a new password</Typography>
    <Typography color="text.secondary" sx={{ my: 2 }}>Use at least 12 characters, including uppercase, lowercase, a number and a symbol.</Typography>
    {!link.token && <Alert severity="warning">Open a valid password-reset link to continue.</Alert>}
    {message && <Alert severity="success">{message}</Alert>}
    {error && <Alert severity="error">{error}</Alert>}
    <Stack component="form" onSubmit={submit} spacing={2} sx={{ mt: 2 }}>
      <TextField label="New password" type="password" autoComplete="new-password" value={password} onChange={e => setPassword(e.target.value)} required />
      <TextField label="Confirm password" type="password" autoComplete="new-password" value={confirm} onChange={e => setConfirm(e.target.value)} required />
      <Button type="submit" variant="contained" disabled={busy || !link.token || !!message}>Save new password</Button>
      <Link component={RouterLink} to="/login">Back to sign in</Link>
    </Stack>
  </Paper></Box>;
}
