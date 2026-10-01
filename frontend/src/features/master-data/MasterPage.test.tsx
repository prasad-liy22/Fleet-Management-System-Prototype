import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import App from '../../App';
import { sessionKey } from '../../auth/AuthProvider';
import { setAccessToken } from '../../api/client';
import { MasterPage } from './MasterPage';
import { vehicles, drivers, customers } from './config';
import type { MasterConfig, MasterRecord } from './types';
const json = (data: unknown, status = 200) => new Response(JSON.stringify(data), { status, headers: { 'Content-Type': 'application/json' } });
const audit = { createdBy: 'admin', updatedBy: 'admin', createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-01T00:00:00Z' };
const records: Record<string, MasterRecord> = {
  vehicles: { id: 'vehicle-id', registrationNumber: 'TEST-001', model: 'Fictional Hauler', year: 2024, capacity: 18000, currentOdometer: 150, status: 'Available', version: 12, isDeleted: false, audit },
  drivers: { id: 'driver-id', name: 'Sample Driver', licenceNumber: 'TEST-LICENCE', contact: '+1 555 010 0042', status: 'Available', linkedAccount: null, version: 12, isDeleted: false, audit },
  customers: { id: 'customer-id', companyName: 'Fictional Company', contactPerson: 'Sample Contact', telephone: '+1 555 010 0042', email: 'sample@fictional.example', address: 'Sample road', version: 12, isDeleted: false, audit },
};
const page = (items: MasterRecord[], totalCount = items.length) => ({ items, page: 1, pageSize: 20, totalCount });
beforeEach(() => { sessionStorage.clear(); setAccessToken(null); });
function show(config: MasterConfig) { render(<MasterPage config={config} />); }
function mockList(config: MasterConfig, items = [records[config.endpoint]]) {
  const mock = vi.fn((url: string, init?: RequestInit) => {
    if (init?.method) return Promise.resolve(json({ ...records[config.endpoint], version: 13 }));
    if (url.includes('/' + records[config.endpoint].id)) return Promise.resolve(json(records[config.endpoint]));
    return Promise.resolve(json(page(items)));
  });
  vi.stubGlobal('fetch', mock); return mock;
}
function fill(config: MasterConfig) {
  for (const field of config.fields) {
    if (field.options) continue;
    fireEvent.change(screen.getByLabelText(field.label, { exact: false }), { target: { value: String(records[config.endpoint][field.key] ?? '') } });
  }
}
for (const config of [vehicles, drivers, customers]) describe(config.title, () => {
  it('renders API records and a read-only detail view', async () => {
    mockList(config); show(config);
    const name = String(records[config.endpoint][config.nameKey]);
    expect(await screen.findByRole('table', { name: config.title })).toBeInTheDocument();
    expect(screen.getByText(name)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'View ' + name }));
    const dialog = await screen.findByRole('dialog');
    expect(within(dialog).getByLabelText(config.fields[0].label, { exact: false })).toBeDisabled();
    expect(within(dialog).queryByRole('button', { name: /Save / })).not.toBeInTheDocument();
  });
  it('validates required fields before posting and creates a record', async () => {
    const fetchMock = mockList(config, []); show(config);
    fireEvent.click(screen.getByRole('button', { name: 'Add ' + config.singular }));
    fireEvent.click(screen.getByRole('button', { name: 'Save ' + config.singular.toLowerCase() }));
    expect(await screen.findByText(config.fields[0].label + ' is required.')).toBeInTheDocument();
    expect(fetchMock.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(false);
    fill(config);
    fireEvent.click(screen.getByRole('button', { name: 'Save ' + config.singular.toLowerCase() }));
    expect(await screen.findByText(config.singular + ' saved.')).toBeInTheDocument();
    const call = fetchMock.mock.calls.find(([, init]) => init?.method === 'POST')!;
    expect(call[0]).toBe('/api/' + config.endpoint);
    expect(JSON.parse(call[1]!.body as string)).toHaveProperty(config.nameKey, records[config.endpoint][config.nameKey]);
  });
  it('edits using the loaded concurrency version', async () => {
    const fetchMock = mockList(config); show(config);
    fireEvent.click(await screen.findByRole('button', { name: 'Edit ' + records[config.endpoint][config.nameKey] }));
    const field = await screen.findByLabelText(config.fields[0].label, { exact: false });
    fireEvent.change(field, { target: { value: 'Updated record' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save ' + config.singular.toLowerCase() }));
    await screen.findByText(config.singular + ' saved.');
    const saved = fetchMock.mock.calls.find(([, init]) => init?.method === 'PUT')!;
    expect(JSON.parse(saved[1]!.body as string)).toMatchObject({ version: 12, [config.fields[0].key]: 'Updated record' });
  });
  it('requires confirmation for deactivation and submits the version', async () => {
    const fetchMock = mockList(config); show(config);
    fireEvent.click(await screen.findByRole('button', { name: 'Deactivate ' + records[config.endpoint][config.nameKey] }));
    expect(screen.getByText(/Historical records will be preserved/)).toBeInTheDocument();
    expect(fetchMock.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(false);
    fireEvent.click(screen.getByRole('button', { name: 'Confirm' }));
    expect(await screen.findByText(config.singular + ' deactivated.')).toBeInTheDocument();
    const action = fetchMock.mock.calls.find(([, init]) => init?.method === 'POST')!;
    expect(action[0]).toContain('/deactivate');
    expect(JSON.parse(action[1]!.body as string)).toEqual({ version: 12 });
  });
});
it('rejects invalid capacity and odometer in the vehicle form', async () => {
  const mock = mockList(vehicles, []); show(vehicles);
  fireEvent.click(screen.getByRole('button', { name: 'Add Vehicle' })); fill(vehicles);
  fireEvent.change(screen.getByLabelText('Capacity (kg)', { exact: false }), { target: { value: '0' } });
  fireEvent.change(screen.getByLabelText('Current odometer (km)', { exact: false }), { target: { value: '-1' } });
  fireEvent.click(screen.getByRole('button', { name: 'Save vehicle' }));
  expect(await screen.findAllByText('Enter a valid number in the supported range.')).toHaveLength(2);
  expect(mock.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(false);
});
it('validates optional customer email when supplied', async () => {
  const mock = mockList(customers, []); show(customers);
  fireEvent.click(screen.getByRole('button', { name: 'Add Customer' })); fill(customers);
  fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'invalid' } });
  fireEvent.click(screen.getByRole('button', { name: 'Save customer' }));
  expect(await screen.findByText('Enter a valid email address.')).toBeInTheDocument();
  expect(mock.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(false);
});
it('shows loading, API failure, retry, and empty states', async () => {
  let attempts = 0;
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(++attempts === 1 ? json({ title: 'Temporary database error' }, 503) : json(page([])))));
  show(drivers);
  expect(screen.getByRole('status', { name: 'Loading drivers' })).toBeInTheDocument();
  expect(await screen.findByRole('alert')).toHaveTextContent('Temporary database error');
  fireEvent.click(screen.getByRole('button', { name: 'Reload' }));
  expect(await screen.findByText('No drivers match these filters.')).toBeInTheDocument();
});
it('sends pagination, search and status filtering to the API', async () => {
  const mock = vi.fn(() => Promise.resolve(json(page([records.vehicles], 45)))); vi.stubGlobal('fetch', mock);
  show(vehicles); await screen.findByRole('table');
  fireEvent.click(screen.getByRole('button', { name: 'Go to next page' }));
  await waitFor(() => expect(mock).toHaveBeenLastCalledWith(expect.stringContaining('page=2'), expect.anything()));
  fireEvent.change(screen.getByLabelText('Search vehicles'), { target: { value: 'TEST' } });
  await waitFor(() => expect(mock).toHaveBeenLastCalledWith(expect.stringMatching(/page=1.*search=TEST/), expect.anything()));
  fireEvent.mouseDown(screen.getByRole('combobox', { name: 'Filter status' }));
  fireEvent.click(screen.getByRole('option', { name: 'Under Maintenance' }));
  await waitFor(() => expect(mock).toHaveBeenLastCalledWith(expect.stringContaining('status=UnderMaintenance'), expect.anything()));
});
it('offers conflict reload and uses the refreshed version on the next save', async () => {
  let reads = 0; let writes = 0;
  const mock = vi.fn((url: string, init?: RequestInit) => {
    if (init?.method === 'PUT') return Promise.resolve(++writes === 1 ? json({ title: 'This record changed.' }, 409) : json(records.drivers));
    if (url.includes('/driver-id')) return Promise.resolve(json({ ...records.drivers, version: ++reads === 1 ? 12 : 99 }));
    return Promise.resolve(json(page([records.drivers])));
  }); vi.stubGlobal('fetch', mock); show(drivers);
  fireEvent.click(await screen.findByRole('button', { name: 'Edit Sample Driver' }));
  fireEvent.change(await screen.findByLabelText('Name', { exact: false }), { target: { value: 'Edited driver' } });
  fireEvent.click(screen.getByRole('button', { name: 'Save driver' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('This record changed.');
  fireEvent.click(screen.getByRole('button', { name: 'Reload latest record (discard edits)' }));
  await waitFor(() => expect(screen.getByLabelText('Name', { exact: false })).toHaveValue('Sample Driver'));
  fireEvent.click(screen.getByRole('button', { name: 'Save driver' }));
  await screen.findByText('Driver saved.');
  expect(JSON.parse(mock.mock.calls.filter(([, init]) => init?.method === 'PUT')[1][1]!.body as string)).toHaveProperty('version', 99);
});
it('includes deactivated records on request and confirms reactivation', async () => {
  const mock = mockList(customers, [{ ...records.customers, isDeleted: true }]); show(customers);
  fireEvent.click(screen.getByRole('switch', { name: 'Include deactivated' }));
  fireEvent.click(await screen.findByRole('button', { name: 'Reactivate Fictional Company' }));
  expect(mock.mock.calls.some(([url]) => url.includes('includeDeleted=true'))).toBe(true);
  fireEvent.click(screen.getByRole('button', { name: 'Confirm' }));
  await screen.findByText('Customer reactivated.');
  expect(mock.mock.calls.some(([url, init]) => url.endsWith('/reactivate') && init?.method === 'POST')).toBe(true);
});
it.each(['/vehicles', '/drivers', '/customers'])('rejects other roles at route %s without loading master data', async path => {
  sessionStorage.setItem(sessionKey, JSON.stringify({ token: 'test', expiresAt: new Date(Date.now() + 900000).toISOString() }));
  const mock = vi.fn(() => Promise.resolve(json({ id: 'owner', displayName: 'Owner', role: 'FleetOwner', email: 'owner@fleet.example' })));
  vi.stubGlobal('fetch', mock);
  render(<MemoryRouter initialEntries={[path]}><App /></MemoryRouter>);
  expect(await screen.findByText(/This page is not available for your role/)).toBeInTheDocument();
  expect(mock.mock.calls).toHaveLength(1);
});
