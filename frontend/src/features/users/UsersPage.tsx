import { useEffect, useState } from 'react';
import { Alert, Box, Button, Chip, CircularProgress, MenuItem, Paper, Snackbar, Table, TableBody, TableCell, TableContainer, TableHead, TablePagination, TableRow, TextField, Typography } from '@mui/material';
import { request } from '../../api/client';
import { useAuth } from '../../auth/AuthProvider';
import { roleConfig } from '../../auth/roleConfig';
import { roles, type Page, type UserAccount } from '../../auth/types';
import { UserDialog } from './UserDialog';

export function UsersPage() {
  const { user, logout } = useAuth();
  const [data, setData] = useState<Page<UserAccount> | null>(null);
  const [search, setSearch] = useState('');
  const [role, setRole] = useState('');
  const [active, setActive] = useState('');
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(20);
  const [revision, setRevision] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [editing, setEditing] = useState<UserAccount | null | undefined>(undefined);

  useEffect(() => {
    const controller = new AbortController();
    const params = new URLSearchParams({ page: String(page + 1), pageSize: String(pageSize), search });
    if (role) params.set('role', role);
    if (active) params.set('isActive', active);
    setLoading(true); setError('');
    const timer = window.setTimeout(() => {
      request<Page<UserAccount>>('/users?' + params, { signal: controller.signal })
        .then(setData).catch(cause => {
          if (!controller.signal.aborted) setError(cause instanceof Error ? cause.message : 'Unable to load users.');
        }).finally(() => { if (!controller.signal.aborted) setLoading(false); });
    }, 200);
    return () => { controller.abort(); window.clearTimeout(timer); };
  }, [search, role, active, page, pageSize, revision]);

  async function edit(id: string) {
    try { setEditing(await request<UserAccount>('/users/' + id)); }
    catch (cause) { setError(cause instanceof Error ? cause.message : 'Unable to load account.'); }
  }
  function saved(account: UserAccount) {
    setEditing(undefined);
    if (user?.id === account.id) { void logout().catch(() => {}); return; }
    setNotice('Account saved.');
    setRevision(value => value + 1);
  }
  return <>
    <Box className="flex flex-wrap items-center justify-between gap-4" sx={{ mb: 3 }}>
      <div><Typography variant="overline" color="primary">ACCESS ADMINISTRATION</Typography>
        <Typography variant="h4" fontWeight={700}>Users</Typography></div>
      <Button variant="contained" onClick={() => setEditing(null)}>Create user</Button>
    </Box>
    <Box className="flex flex-wrap gap-3" sx={{ mb: 3 }}>
      <TextField label="Search users" value={search} onChange={e => { setSearch(e.target.value); setPage(0); }} inputProps={{ maxLength: 160 }} />
      <TextField select label="Filter role" value={role} onChange={e => { setRole(e.target.value); setPage(0); }} sx={{ minWidth: 220 }}>
        <MenuItem value="">All roles</MenuItem>{roles.map(item => <MenuItem key={item} value={item}>{roleConfig[item].label}</MenuItem>)}
      </TextField>
      <TextField select label="Filter status" value={active} onChange={e => { setActive(e.target.value); setPage(0); }} sx={{ minWidth: 160 }}>
        <MenuItem value="">All statuses</MenuItem><MenuItem value="true">Active</MenuItem><MenuItem value="false">Inactive</MenuItem>
      </TextField>
    </Box>
    {error && <Alert severity="error" action={<Button onClick={() => setRevision(v => v + 1)}>Retry</Button>} sx={{ mb: 2 }}>{error}</Alert>}
    {loading ? <Box role="status" aria-label="Loading users" sx={{ p: 4 }}><CircularProgress /></Box> : error ? null :
      <Paper variant="outlined"><TableContainer><Table aria-label="User accounts">
        <TableHead><TableRow><TableCell>Name</TableCell><TableCell>Email</TableCell><TableCell>Role</TableCell><TableCell>Status</TableCell><TableCell>Action</TableCell></TableRow></TableHead>
        <TableBody>{data?.items.map(account => <TableRow key={account.id}>
          <TableCell>{account.displayName}</TableCell><TableCell>{account.email}</TableCell>
          <TableCell>{roleConfig[account.role]?.label ?? account.role}</TableCell>
          <TableCell><Chip size="small" label={account.isActive ? 'Active' : 'Inactive'} color={account.isActive ? 'success' : 'default'} /></TableCell>
          <TableCell><Button onClick={() => void edit(account.id)} aria-label={'Edit ' + account.displayName}>Edit</Button></TableCell>
        </TableRow>)}
          {!data?.items.length && <TableRow><TableCell colSpan={5}>No users match these filters.</TableCell></TableRow>}
        </TableBody>
      </Table></TableContainer>
        <TablePagination component="div" count={data?.totalCount ?? 0} page={page} rowsPerPage={pageSize} rowsPerPageOptions={[10, 20, 50]}
          onPageChange={(_, value) => setPage(value)} onRowsPerPageChange={e => { setPageSize(Number(e.target.value)); setPage(0); }} />
      </Paper>}
    {editing !== undefined && <UserDialog account={editing} onClose={() => setEditing(undefined)} onSaved={saved} />}
    <Snackbar open={!!notice} autoHideDuration={4000} onClose={() => setNotice('')} message={notice} />
  </>;
}
