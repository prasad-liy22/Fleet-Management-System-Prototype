import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import App from '../App';
import { sessionKey } from './AuthProvider';
import { sessionExpiredEvent, setAccessToken } from '../api/client';
import type { CurrentUser, Role } from './types';

const owner: CurrentUser = { id: 'owner-id', displayName: 'Sample Owner', email: 'owner@fleet.example', role: 'FleetOwner', driverId: null };
function json(data: unknown, status = 200) { return new Response(JSON.stringify(data), { status, headers: { 'Content-Type': 'application/json' } }); }
function session() { sessionStorage.setItem(sessionKey, JSON.stringify({ token: 'test-token', expiresAt: new Date(Date.now() + 900000).toISOString() })); }
function show(path = '/') { return render(<MemoryRouter initialEntries={[path]}><App /></MemoryRouter>); }
function fillLogin() {
  fireEvent.change(screen.getByLabelText('Email', { exact: false }), { target: { value: owner.email } });
  fireEvent.change(screen.getByLabelText('Password', { exact: false }), { target: { value: 'FictionalPassword!42' } });
  fireEvent.click(screen.getByRole('button', { name: 'Sign in' }));
}
beforeEach(() => { sessionStorage.clear(); setAccessToken(null); });

describe('Authentication experience', () => {
  it('redirects an anonymous protected route to the login form', async () => {
    show('/users');
    expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Create user' })).not.toBeInTheDocument();
  });

  it('signs in, stores the tab session and displays the correct role landing page', async () => {
    const fetchMock = vi.fn().mockResolvedValue(json({ accessToken: 'test-token', expiresAt: new Date(Date.now() + 900000).toISOString(), user: owner }));
    vi.stubGlobal('fetch', fetchMock);
    show('/login');
    await screen.findByRole('heading', { name: 'Welcome back' });
    fillLogin();
    expect(await screen.findByRole('heading', { name: 'Hello, Sample Owner' })).toBeInTheDocument();
    expect(sessionStorage.getItem(sessionKey)).toContain('test-token');
    const init = fetchMock.mock.calls[0][1] as RequestInit;
    expect(new Headers(init.headers).has('Authorization')).toBe(false);
  });

  it('shows a generic failed-login response without authenticating', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(json({ title: 'Unable to sign in with these credentials.' }, 401)));
    show('/login');
    await screen.findByRole('heading', { name: 'Welcome back' });
    fillLogin();
    expect(await screen.findByRole('alert')).toHaveTextContent('Unable to sign in');
    expect(sessionStorage.getItem(sessionKey)).toBeNull();
  });

  it('waits for current-user verification before showing protected content', () => {
    session();
    vi.stubGlobal('fetch', vi.fn(() => new Promise(() => {})));
    show('/');
    expect(screen.getByRole('status', { name: 'Checking session' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: /Hello/ })).not.toBeInTheDocument();
  });

  it('restores identity through me and attaches the bearer token', async () => {
    session();
    const fetchMock = vi.fn().mockResolvedValue(json(owner));
    vi.stubGlobal('fetch', fetchMock);
    show('/');
    expect(await screen.findByRole('heading', { name: 'Hello, Sample Owner' })).toBeInTheDocument();
    expect(fetchMock.mock.calls[0][0]).toBe('/api/auth/me');
    expect(new Headers((fetchMock.mock.calls[0][1] as RequestInit).headers).get('Authorization')).toBe('Bearer test-token');
  });

  it('rejects an invalid restored session', async () => {
    session();
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(json({ title: 'Session expired' }, 401)));
    show('/');
    expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeInTheDocument();
    expect(sessionStorage.getItem(sessionKey)).toBeNull();
  });

  it('shows the unauthorized page for a cross-role URL', async () => {
    session(); vi.stubGlobal('fetch', vi.fn().mockResolvedValue(json(owner)));
    show('/users');
    expect(await screen.findByRole('heading', { name: 'This page is not available for your role.' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Create user' })).not.toBeInTheDocument();
  });

  it.each([
    ['FleetAdministrator', 'Users', 'Reports'],
    ['OperationsCoordinator', 'Orders', 'Users'],
    ['Mechanic', 'Maintenance', 'Orders'],
    ['Driver', 'My Trips', 'Users'],
    ['FleetOwner', 'Reports', 'Maintenance'],
  ] as const)('shows only relevant navigation for %s', async (role: Role, visible, hidden) => {
    session(); vi.stubGlobal('fetch', vi.fn().mockResolvedValue(json({ ...owner, role })));
    show('/');
    const nav = await screen.findByRole('navigation', { name: 'Main navigation' });
    expect(within(nav).getByText(visible)).toBeInTheDocument();
    expect(within(nav).queryByText(hidden)).not.toBeInTheDocument();
    if (role !== 'FleetAdministrator') expect(within(nav).getByText(visible)).toHaveAttribute('aria-disabled', 'true');
  });

  it('logs out through the API and clears the local session', async () => {
    session();
    const fetchMock = vi.fn().mockResolvedValueOnce(json(owner)).mockResolvedValueOnce(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);
    show('/');
    fireEvent.click(await screen.findByRole('button', { name: 'Sign out' }));
    expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeInTheDocument();
    expect(fetchMock.mock.calls[1][0]).toBe('/api/auth/logout');
    expect(sessionStorage.getItem(sessionKey)).toBeNull();
  });

  it('clears a session when a protected API reports expiration', async () => {
    session(); vi.stubGlobal('fetch', vi.fn().mockResolvedValue(json(owner)));
    show('/');
    await screen.findByRole('heading', { name: 'Hello, Sample Owner' });
    fireEvent(window, new Event(sessionExpiredEvent));
    await waitFor(() => expect(screen.getByRole('heading', { name: 'Welcome back' })).toBeInTheDocument());
    expect(screen.getByRole('alert')).toHaveTextContent('session has expired');
  });

  it('requests password reset and displays the generic acknowledgement', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(json({ message: 'If the account is eligible, instructions will be delivered.' })));
    show('/forgot-password');
    fireEvent.change(screen.getByLabelText('Email', { exact: false }), { target: { value: owner.email } });
    fireEvent.click(screen.getByRole('button', { name: 'Request reset' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('If the account is eligible');
  });
});
