import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import App from '../../App';
import { sessionKey } from '../../auth/AuthProvider';
import { setAccessToken } from '../../api/client';
import type { UserAccount } from '../../auth/types';

const admin = { id: 'admin-id', displayName: 'Sample Administrator', email: 'admin@fleet.example', role: 'FleetAdministrator', driverId: null };
const account: UserAccount = {
  id: 'user-id', displayName: 'Sample Account', email: 'sample@fleet.example', role: 'FleetOwner', driverId: null,
  phoneNumber: null, isActive: true, version: 0, createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-01T00:00:00Z',
};
const json = (data: unknown, status = 200) => new Response(JSON.stringify(data), { status, headers: { 'Content-Type': 'application/json' } });
beforeEach(() => {
  sessionStorage.clear(); setAccessToken(null);
  sessionStorage.setItem(sessionKey, JSON.stringify({ token: 'admin-test-token', expiresAt: new Date(Date.now() + 900000).toISOString() }));
});
function show() { render(<MemoryRouter initialEntries={['/users']}><App /></MemoryRouter>); }

describe('User administration', () => {
  it('loads user records and requires confirmation before deactivation', async () => {
    const fetchMock = vi.fn((url: string, init?: RequestInit) => {
      if (url.endsWith('/auth/me')) return Promise.resolve(json(admin));
      if (init?.method === 'PUT') return Promise.resolve(json({ ...account, isActive: false, version: 1 }));
      if (url.endsWith('/users/user-id')) return Promise.resolve(json(account));
      return Promise.resolve(json({ items: [account], page: 1, pageSize: 20, totalCount: 1 }));
    });
    vi.stubGlobal('fetch', fetchMock);
    show();
    fireEvent.click(await screen.findByRole('button', { name: 'Edit Sample Account' }));
    fireEvent.click(await screen.findByRole('switch', { name: 'Account active' }));
    fireEvent.click(screen.getByRole('button', { name: 'Save account' }));
    expect(await screen.findByText(/Confirm these access changes/)).toBeInTheDocument();
    expect(fetchMock.mock.calls.some(([, init]) => init?.method === 'PUT')).toBe(false);
    fireEvent.click(screen.getByRole('button', { name: 'Confirm changes' }));
    await waitFor(() => expect(fetchMock.mock.calls.some(([, init]) => init?.method === 'PUT')).toBe(true));
    const saved = fetchMock.mock.calls.find(([, init]) => init?.method === 'PUT')![1]!;
    expect(JSON.parse(saved.body as string)).toMatchObject({ isActive: false, version: 0 });
  });

  it('creates a user through the API and displays server validation errors', async () => {
    const fetchMock = vi.fn((url: string, init?: RequestInit) => {
      if (url.endsWith('/auth/me')) return Promise.resolve(json(admin));
      if (init?.method === 'POST') return Promise.resolve(json({ title: 'The email already belongs to an account.' }, 409));
      return Promise.resolve(json({ items: [], page: 1, pageSize: 20, totalCount: 0 }));
    });
    vi.stubGlobal('fetch', fetchMock);
    show();
    fireEvent.click(await screen.findByRole('button', { name: 'Create user' }));
    fireEvent.change(screen.getByLabelText('Display name', { exact: false }), { target: { value: 'Sample New User' } });
    fireEvent.change(screen.getByLabelText('Email', { exact: false }), { target: { value: 'sample@fleet.example' } });
    fireEvent.change(screen.getByLabelText('Initial password', { exact: false }), { target: { value: 'FictionalPassword!42' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save account' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('email already belongs');
  });

  it('displays a loading state and handles a failed user list with retry', async () => {
    let attempts = 0;
    vi.stubGlobal('fetch', vi.fn((url: string) => {
      if (url.endsWith('/auth/me')) return Promise.resolve(json(admin));
      attempts++;
      return Promise.resolve(attempts === 1 ? json({ title: 'Database temporarily unavailable.' }, 503)
        : json({ items: [], page: 1, pageSize: 20, totalCount: 0 }));
    }));
    show();
    expect(await screen.findByRole('status', { name: 'Loading users' })).toBeInTheDocument();
    expect(await screen.findByRole('alert')).toHaveTextContent('temporarily unavailable');
    fireEvent.click(screen.getByRole('button', { name: 'Retry' }));
    await waitFor(() => expect(attempts).toBe(2));
    expect(await screen.findByText('No users match these filters.')).toBeInTheDocument();
  });
});
