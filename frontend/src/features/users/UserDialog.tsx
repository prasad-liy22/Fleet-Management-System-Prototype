import { useEffect, useState, type FormEvent } from 'react';
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, FormControlLabel, MenuItem, Stack, Switch, TextField } from '@mui/material';
import { request } from '../../api/client';
import { roleConfig } from '../../auth/roleConfig';
import { roles, type DriverOption, type Role, type UserAccount } from '../../auth/types';

export function UserDialog({ account, onClose, onSaved }: {
  account: UserAccount | null; onClose(): void; onSaved(account: UserAccount): void;
}) {
  const [displayName, setName] = useState(account?.displayName ?? '');
  const [email, setEmail] = useState(account?.email ?? '');
  const [phoneNumber, setPhone] = useState(account?.phoneNumber ?? '');
  const [role, setRole] = useState<Role>(account?.role ?? 'FleetOwner');
  const [driverId, setDriverId] = useState(account?.driverId ?? '');
  const [password, setPassword] = useState('');
  const [isActive, setActive] = useState(account?.isActive ?? true);
  const [drivers, setDrivers] = useState<DriverOption[]>([]);
  const [driverSearch, setDriverSearch] = useState('');
  const [loadingDrivers, setLoadingDrivers] = useState(false);
  const [busy, setBusy] = useState(false);
  const [confirm, setConfirm] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (role !== 'Driver') { setLoadingDrivers(false); return; }
    const controller = new AbortController();
    const params = new URLSearchParams({ search: driverSearch });
    if (account) params.set('userId', account.id);
    setLoadingDrivers(true);
    const timer = window.setTimeout(() => {
      request<DriverOption[]>('/users/driver-options?' + params, { signal: controller.signal })
        .then(setDrivers).catch(cause => {
          if (!controller.signal.aborted) setError(cause instanceof Error ? cause.message : 'Unable to load drivers.');
        }).finally(() => { if (!controller.signal.aborted) setLoadingDrivers(false); });
    }, 200);
    return () => { controller.abort(); window.clearTimeout(timer); };
  }, [role, account, driverSearch]);

  async function save() {
    setBusy(true); setError('');
    const data = { displayName, email, phoneNumber: phoneNumber || null, role, driverId: role === 'Driver' ? driverId || null : null };
    try {
      const saved = await request<UserAccount>(account ? '/users/' + account.id : '/users', {
        method: account ? 'PUT' : 'POST',
        body: JSON.stringify(account ? { ...data, isActive, version: account.version } : { ...data, password }),
      });
      onSaved(saved);
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'Unable to save account.'); setConfirm(false); }
    finally { setBusy(false); }
  }
  function submit(event: FormEvent) {
    event.preventDefault();
    if (account && (role !== account.role || isActive !== account.isActive || (driverId || null) !== account.driverId))
      setConfirm(true);
    else void save();
  }
  return <Dialog open onClose={busy ? undefined : onClose} fullWidth maxWidth="sm">
    <DialogTitle>{account ? 'Edit account' : 'Create account'}</DialogTitle>
    <form onSubmit={submit}>
      <DialogContent>
        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        {confirm ? <Alert severity="warning">Confirm these access changes. Existing sessions for this account will be invalidated.</Alert> :
          <Stack spacing={2} sx={{ pt: 1 }}>
            <TextField label="Display name" value={displayName} onChange={e => setName(e.target.value)} required inputProps={{ maxLength: 160 }} />
            <TextField label="Email" type="email" value={email} onChange={e => setEmail(e.target.value)} required inputProps={{ maxLength: 254 }} />
            <TextField label="Phone / contact" value={phoneNumber} onChange={e => setPhone(e.target.value)} inputProps={{ maxLength: 40 }} />
            <TextField select label="Role" value={role} onChange={e => { setRole(e.target.value as Role); setDriverId(''); }}>
              {roles.map(item => <MenuItem key={item} value={item}>{roleConfig[item].label}</MenuItem>)}
            </TextField>
            {role === 'Driver' && <>
              <TextField label="Find driver" value={driverSearch} onChange={e => setDriverSearch(e.target.value)} inputProps={{ maxLength: 160 }} />
              <TextField select label="Linked driver" value={driverId} onChange={e => setDriverId(e.target.value)}
                required disabled={loadingDrivers} helperText={loadingDrivers ? 'Loading driver records…' : 'Select an existing driver record.'}>
                <MenuItem value="">Select driver</MenuItem>
                {driverId && !drivers.some(d => d.id === driverId) && <MenuItem value={driverId}>Current selection</MenuItem>}
                {drivers.map(driver => <MenuItem key={driver.id} value={driver.id}>{driver.name} · {driver.licenceNumber}</MenuItem>)}
              </TextField>
            </>}
            {!account && <TextField label="Initial password" type="password" autoComplete="new-password" value={password}
              onChange={e => setPassword(e.target.value)} required helperText="At least 12 characters with uppercase, lowercase, a number and a symbol." />}
            {account && <FormControlLabel control={<Switch checked={isActive} onChange={e => setActive(e.target.checked)} />} label="Account active" />}
          </Stack>}
      </DialogContent>
      <DialogActions>
        <Button onClick={confirm ? () => setConfirm(false) : onClose} disabled={busy}>{confirm ? 'Back' : 'Cancel'}</Button>
        {confirm ? <Button onClick={() => void save()} variant="contained" disabled={busy}>Confirm changes</Button> :
          <Button type="submit" variant="contained" disabled={busy || loadingDrivers}>{busy ? 'Saving…' : 'Save account'}</Button>}
      </DialogActions>
    </form>
  </Dialog>;
}
