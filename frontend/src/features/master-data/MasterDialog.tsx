import { useState, type FormEvent } from 'react';
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem, Stack, TextField, Typography } from '@mui/material';
import { ApiError, request } from '../../api/client';
import { label, type MasterConfig, type MasterRecord } from './types';

type Values = Record<string, string>;
function initial(config: MasterConfig, record: MasterRecord | null): Values {
  return Object.fromEntries(config.fields.map(field => [field.key, String(record?.[field.key] ??
    (field.key === 'status' ? 'Available' : field.key === 'currentOdometer' ? 0 : ''))]));
}
export function MasterDialog({ config, record, readOnly, onClose, onSaved }: {
  config: MasterConfig; record: MasterRecord | null; readOnly: boolean; onClose(): void; onSaved(): void;
}) {
  const [current, setCurrent] = useState(record);
  const [values, setValues] = useState(() => initial(config, record));
  const [errors, setErrors] = useState<Values>({});
  const [error, setError] = useState('');
  const [conflict, setConflict] = useState(false);
  const [busy, setBusy] = useState(false);
  const locked = readOnly || !!current?.isDeleted;
  async function reload() {
    if (!current) return;
    setBusy(true);
    try {
      const latest = await request<MasterRecord>(`/${config.endpoint}/${current.id}?includeDeleted=true`);
      setCurrent(latest); setValues(initial(config, latest)); setError(''); setErrors({}); setConflict(false);
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'Unable to reload.'); }
    finally { setBusy(false); }
  }
  async function save(event: FormEvent) {
    event.preventDefault();
    const issues: Values = {};
    for (const field of config.fields) {
      const value = values[field.key].trim();
      if (field.required && !value) issues[field.key] = `${field.label} is required.`;
      else if (field.maxLength && value.length > field.maxLength) issues[field.key] = `Use at most ${field.maxLength} characters.`;
      else if (field.type === 'number' && (!value || !Number.isFinite(Number(value)) || Number(value) < (field.min ?? 0) ||
        Number(value) > (field.max ?? Infinity) || (field.step === 1 && !Number.isInteger(Number(value))) ||
        (field.step === 0.01 && !/^\d+(\.\d{1,2})?$/.test(value)))) issues[field.key] = 'Enter a valid number in the supported range.';
      else if (field.type === 'email' && value && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) issues[field.key] = 'Enter a valid email address.';
      else if (['contact', 'telephone'].includes(field.key) && value && (!/\d/.test(value) || /[^\d +()\-./xX#]/.test(value)))
        issues[field.key] = 'Enter a phone number; international prefixes and extensions are supported.';
    }
    if (current?.currentOdometer !== undefined && Number(values.currentOdometer) < current.currentOdometer)
      issues.currentOdometer = 'Odometer cannot be reduced.';
    setErrors(issues);
    if (Object.keys(issues).length) return;
    setBusy(true); setError(''); setConflict(false);
    const body: Record<string, string | number> = Object.fromEntries(config.fields.map(field =>
      [field.key, field.type === 'number' ? Number(values[field.key]) : values[field.key].trim()]));
    if (config.endpoint === 'vehicles') body.status = current?.status ?? 'Available';
    if (current) body.version = current.version;
    try {
      await request(`/${config.endpoint}${current ? '/' + current.id : ''}`, {
        method: current ? 'PUT' : 'POST', body: JSON.stringify(body),
      });
      onSaved();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Unable to save.');
      setConflict(cause instanceof ApiError && cause.status === 409);
    } finally { setBusy(false); }
  }
  return <Dialog open onClose={busy ? undefined : onClose} fullWidth maxWidth="sm">
    <DialogTitle>{locked ? 'View' : current ? 'Edit' : 'Add'} {config.singular}</DialogTitle>
    <form onSubmit={save} noValidate>
      <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
        {error && <Alert severity="error">{error}</Alert>}
        {conflict && current && <Button disabled={busy} onClick={() => void reload()}>Reload latest record (discard edits)</Button>}
        {current?.isDeleted && <Alert severity="info">This record is deactivated. Reactivate it from the list before editing.</Alert>}
        {config.endpoint === 'vehicles' && <Alert severity="info">Status: {label(current?.status ?? 'Available')}. Trip and maintenance workflows control this status.</Alert>}
        {config.fields.map(field => <TextField key={field.key} label={field.label} required={field.required}
          value={values[field.key]} type={field.type ?? 'text'} select={!!field.options} disabled={locked || busy}
          onChange={event => setValues(v => ({ ...v, [field.key]: event.target.value }))}
          error={!!errors[field.key]} helperText={errors[field.key]}
          inputProps={{ maxLength: field.maxLength, min: field.key === 'currentOdometer' ? current?.currentOdometer ?? field.min : field.min, max: field.max, step: field.step }}>
          {field.options && [...new Set([...field.options, ...(current?.status ? [current.status] : [])])]
            .map(option => <MenuItem key={option} value={option} disabled={option === 'OnTrip'}>{label(option)}</MenuItem>)}
        </TextField>)}
        {config.endpoint === 'drivers' && <Typography variant="body2">Linked account: {current?.linkedAccount
          ? `${current.linkedAccount.email} (${current.linkedAccount.isActive ? 'Active' : 'Inactive'})` : 'No linked account'}. Manage account links in Users.</Typography>}
        {current && <Typography variant="caption">Created {new Date(current.audit.createdAt).toLocaleString()} by {current.audit.createdBy}. Updated {new Date(current.audit.updatedAt).toLocaleString()} by {current.audit.updatedBy}.</Typography>}
      </Stack></DialogContent>
      <DialogActions><Button disabled={busy} onClick={onClose}>{locked ? 'Close' : 'Cancel'}</Button>
        {!locked && <Button variant="contained" type="submit" disabled={busy}>{busy ? 'Saving…' : `Save ${config.singular.toLowerCase()}`}</Button>}
      </DialogActions>
    </form>
  </Dialog>;
}
