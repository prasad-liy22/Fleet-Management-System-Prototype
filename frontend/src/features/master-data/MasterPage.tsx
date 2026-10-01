import { useEffect, useState } from 'react';
import { Alert, Box, Button, Chip, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle, FormControlLabel, MenuItem, Paper, Snackbar, Switch, Table, TableBody, TableCell, TableContainer, TableHead, TablePagination, TableRow, TextField, Typography } from '@mui/material';
import { request } from '../../api/client';
import { MasterDialog } from './MasterDialog';
import { label, recordName, type MasterConfig, type MasterRecord, type Page } from './types';

export function MasterPage({ config }: { config: MasterConfig }) {
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [includeDeleted, setIncludeDeleted] = useState(false);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(20);
  const [revision, setRevision] = useState(0);
  const [data, setData] = useState<Page>();
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [actionError, setActionError] = useState('');
  const [notice, setNotice] = useState('');
  const [dialog, setDialog] = useState<{ record: MasterRecord | null; readOnly: boolean }>();
  const [activation, setActivation] = useState<MasterRecord>();
  useEffect(() => {
    const controller = new AbortController();
    setLoading(true); setError('');
    const timer = window.setTimeout(() => {
      const params = new URLSearchParams({ page: String(page + 1), pageSize: String(pageSize), search, includeDeleted: String(includeDeleted) });
      if (status) params.set('status', status);
      request<Page>(`/${config.endpoint}?${params}`, { signal: controller.signal })
        .then(result => {
          if (controller.signal.aborted) return;
          if (page > 0 && !result.items.length) { setPage(Math.max(0, Math.ceil(result.totalCount / pageSize) - 1)); return; }
          setData(result);
        }).catch(cause => { if (!controller.signal.aborted) setError(cause instanceof Error ? cause.message : 'Unable to load records.'); })
        .finally(() => { if (!controller.signal.aborted) setLoading(false); });
    }, 200);
    return () => { window.clearTimeout(timer); controller.abort(); };
  }, [config.endpoint, search, status, includeDeleted, page, pageSize, revision]);
  async function openRecord(record: MasterRecord, readOnly: boolean) {
    setBusy(true); setActionError('');
    try { setDialog({ record: await request<MasterRecord>(`/${config.endpoint}/${record.id}?includeDeleted=true`), readOnly }); }
    catch (cause) { setActionError(cause instanceof Error ? cause.message : 'Unable to load record.'); }
    finally { setBusy(false); }
  }
  async function changeActivation() {
    if (!activation) return;
    setBusy(true); setActionError('');
    try {
      await request(`/${config.endpoint}/${activation.id}/${activation.isDeleted ? 'reactivate' : 'deactivate'}`, {
        method: 'POST', body: JSON.stringify({ version: activation.version }),
      });
      setNotice(`${config.singular} ${activation.isDeleted ? 'reactivated' : 'deactivated'}.`);
      setActivation(undefined); setRevision(v => v + 1);
    } catch (cause) { setActionError(cause instanceof Error ? cause.message : 'Unable to change activation.'); }
    finally { setBusy(false); }
  }
  const columns = config.columns.length + (config.endpoint === 'drivers' ? 1 : 0) + 2;
  return <>
    <Box className="flex flex-wrap items-center justify-between gap-4" sx={{ mb: 3 }}>
      <Box><Typography variant="h4" component="h1">{config.title}</Typography><Typography color="text.secondary">{config.description}</Typography></Box>
      <Button variant="contained" disabled={busy} onClick={() => { setActionError(''); setDialog({ record: null, readOnly: false }); }}>Add {config.singular}</Button>
    </Box>
    <Box className="flex flex-wrap items-center gap-4" sx={{ mb: 3 }}>
      <TextField label={`Search ${config.endpoint}`} value={search} inputProps={{ maxLength: 200 }} onChange={e => { setSearch(e.target.value); setPage(0); }} />
      {config.statuses && <TextField select label="Filter status" value={status} sx={{ minWidth: 180 }} onChange={e => { setStatus(e.target.value); setPage(0); }}>
        <MenuItem value="">All statuses</MenuItem>{config.statuses.map(s => <MenuItem key={s} value={s}>{label(s)}</MenuItem>)}
      </TextField>}
      <FormControlLabel label="Include deactivated" control={<Switch checked={includeDeleted} onChange={(_, value) => { setIncludeDeleted(value); setPage(0); }} />} />
    </Box>
    {(error || (actionError && !activation)) && <Alert severity="error" sx={{ mb: 2 }} action={<Button onClick={() => { setActionError(''); setRevision(v => v + 1); }}>Reload</Button>}>{error || actionError}</Alert>}
    {busy && !dialog && !activation && <Typography role="status">Loading record…</Typography>}
    {loading ? <Box role="status" aria-label={`Loading ${config.endpoint}`} sx={{ p: 4 }}><CircularProgress /></Box> : error ? null :
      <Paper variant="outlined"><TableContainer><Table aria-label={config.title}>
        <TableHead><TableRow>{config.columns.map(key => <TableCell key={key}>{config.fields.find(f => f.key === key)?.label ?? label(key)}</TableCell>)}
          {config.endpoint === 'drivers' && <TableCell>Linked account</TableCell>}<TableCell>Record</TableCell><TableCell>Actions</TableCell>
        </TableRow></TableHead>
        <TableBody>{data?.items.map(record => <TableRow key={record.id}>
          {config.columns.map(key => <TableCell key={key}>{key === 'status' ? <Chip size="small" label={label(record.status ?? '')}
            color={record.status === 'Available' ? 'success' : record.status === 'OnTrip' ? 'info' : 'warning'} /> : String(record[key] ?? '—')}</TableCell>)}
          {config.endpoint === 'drivers' && <TableCell>{record.linkedAccount ? <>{record.linkedAccount.email}<br /><Chip size="small" label={record.linkedAccount.isActive ? 'Account active' : 'Account inactive'} /></> : 'Not linked'}</TableCell>}
          <TableCell><Chip size="small" label={record.isDeleted ? 'Deactivated' : 'Active'} variant="outlined" /></TableCell>
          <TableCell><Box sx={{ display: 'flex', flexWrap: 'wrap' }}>
            <Button disabled={busy} aria-label={`View ${recordName(config, record)}`} onClick={() => void openRecord(record, true)}>View</Button>
            {!record.isDeleted && <Button disabled={busy} aria-label={`Edit ${recordName(config, record)}`} onClick={() => void openRecord(record, false)}>Edit</Button>}
            <Button disabled={busy} color={record.isDeleted ? 'primary' : 'warning'} aria-label={`${record.isDeleted ? 'Reactivate' : 'Deactivate'} ${recordName(config, record)}`}
              onClick={() => { setActionError(''); setActivation(record); }}>{record.isDeleted ? 'Reactivate' : 'Deactivate'}</Button>
          </Box></TableCell>
        </TableRow>)}{!data?.items.length && <TableRow><TableCell colSpan={columns}>No {config.endpoint} match these filters.</TableCell></TableRow>}</TableBody>
      </Table></TableContainer>
        <TablePagination component="div" count={data?.totalCount ?? 0} page={page} rowsPerPage={pageSize} rowsPerPageOptions={[10, 20, 50]}
          onPageChange={(_, value) => setPage(value)} onRowsPerPageChange={e => { setPageSize(Number(e.target.value)); setPage(0); }} />
      </Paper>}
    {dialog && <MasterDialog config={config} {...dialog} onClose={() => setDialog(undefined)} onSaved={() => { setDialog(undefined); setNotice(`${config.singular} saved.`); setRevision(v => v + 1); }} />}
    {activation && <Dialog open onClose={busy ? undefined : () => setActivation(undefined)}>
      <DialogTitle>{activation.isDeleted ? 'Reactivate' : 'Deactivate'} {recordName(config, activation)}?</DialogTitle>
      <DialogContent>{actionError && <Alert severity="error">{actionError}</Alert>}<Typography>
        {activation.isDeleted ? 'Make this record available in normal lists again.' : 'This record will be hidden from normal lists. Historical records will be preserved.'}
      </Typography></DialogContent>
      <DialogActions><Button disabled={busy} onClick={() => { setActivation(undefined); setActionError(''); setRevision(v => v + 1); }}>Cancel / reload list</Button>
        <Button variant="contained" disabled={busy} onClick={() => void changeActivation()}>{busy ? 'Saving…' : 'Confirm'}</Button></DialogActions>
    </Dialog>}
    <Snackbar open={!!notice} message={notice} autoHideDuration={4000} onClose={() => setNotice('')} />
  </>;
}
