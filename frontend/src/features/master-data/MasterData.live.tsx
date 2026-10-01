import { configure, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeAll, beforeEach, expect, inject, it } from 'vitest';
import App from '../../App';
import { setAccessToken } from '../../api/client';

// Explicit opt-in suite, selected by vitest.live.config.ts. Uses real HTTP and PostgreSQL.
// Point this only at an isolated, migrated and seeded development database.
beforeEach(() => { sessionStorage.clear(); setAccessToken(null); });
declare module 'vitest' { export interface ProvidedContext { livePassword: string } }
beforeAll(() => configure({ asyncUtilTimeout: 20000 }));
const suffix = Date.now().toString().slice(-8);
const cases = [
  { route: 'vehicles', singular: 'Vehicle', title: 'Vehicles', fields: { 'Registration number': 'LIVE-P4-TRUCK-' + suffix, Model: 'Live Fictional Hauler', Year: '2024', 'Capacity (kg)': '18000', 'Current odometer (km)': '120' }, name: 'LIVE-P4-TRUCK-' + suffix, editField: 'Model', editValue: 'Updated Live Hauler' },
  { route: 'drivers', singular: 'Driver', title: 'Drivers', fields: { Name: 'Live Sample Driver ' + suffix, 'Licence number': 'LIVE-P4-LICENCE-' + suffix, Contact: '+1 555 010 0042' }, name: 'Live Sample Driver ' + suffix, editField: 'Contact', editValue: '+1 555 010 0043' },
  { route: 'customers', singular: 'Customer', title: 'Customer Companies', fields: { 'Company name': 'Live Fictional Company ' + suffix, 'Contact person': 'Sample Contact', Telephone: '+1 555 010 0042', Email: 'live@fictional.example', Address: 'Sample road' }, name: 'Live Fictional Company ' + suffix, editField: 'Contact person', editValue: 'Updated Sample Contact' },
];
it.each(cases)('renders and manages $route against the live API', async item => {
  const password = inject('livePassword');
  expect(password, 'Supply a private test password for the isolated database').toBeTruthy();
  render(<MemoryRouter initialEntries={['/' + item.route]}><App /></MemoryRouter>);
  fireEvent.change(await screen.findByLabelText('Email', { exact: false }), { target: { value: 'admin@fleet.example' } });
  fireEvent.change(screen.getByLabelText('Password', { exact: false }), { target: { value: password } });
  fireEvent.click(screen.getByRole('button', { name: 'Sign in' }));
  await screen.findByRole('heading', { name: item.title });
  await screen.findByRole('table', { name: item.title });
  fireEvent.click(screen.getByRole('button', { name: 'Add ' + item.singular }));
  const dialog = screen.getByRole('dialog');
  for (const [field, value] of Object.entries(item.fields))
    fireEvent.change(within(dialog).getByLabelText(field, { exact: false }), { target: { value } });
  fireEvent.click(within(dialog).getByRole('button', { name: 'Save ' + item.singular.toLowerCase() }));
  await screen.findByText(item.singular + ' saved.');
  fireEvent.change(screen.getByLabelText('Search ' + item.route), { target: { value: item.name } });
  const view = await screen.findByRole('button', { name: 'View ' + item.name });
  fireEvent.click(view);
  expect(await screen.findByLabelText(Object.keys(item.fields)[0], { exact: false })).toBeDisabled();
  fireEvent.click(screen.getByRole('button', { name: 'Close' }));
  fireEvent.click(screen.getByRole('button', { name: 'Edit ' + item.name }));
  fireEvent.change(await screen.findByLabelText(item.editField, { exact: false }), { target: { value: item.editValue } });
  fireEvent.click(screen.getByRole('button', { name: 'Save ' + item.singular.toLowerCase() }));
  await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
  await screen.findByText(item.editValue);
  fireEvent.click(screen.getByRole('button', { name: 'Deactivate ' + item.name }));
  fireEvent.click(screen.getByRole('button', { name: 'Confirm' }));
  await screen.findByText('No ' + item.route + ' match these filters.');
  fireEvent.click(screen.getByRole('switch', { name: 'Include deactivated' }));
  fireEvent.click(await screen.findByRole('button', { name: 'Reactivate ' + item.name }));
  fireEvent.click(screen.getByRole('button', { name: 'Confirm' }));
  await screen.findByRole('button', { name: 'Edit ' + item.name });
  fireEvent.click(screen.getByRole('button', { name: 'Sign out' }));
  await screen.findByRole('button', { name: 'Sign in' });
}, 60000);
