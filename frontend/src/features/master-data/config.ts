import type { MasterConfig } from './types';
export const vehicles: MasterConfig = {
  endpoint: 'vehicles', title: 'Vehicles', singular: 'Vehicle', nameKey: 'registrationNumber',
  description: 'Manage vehicle records, carrying capacity and odometer readings.',
  statuses: ['Available', 'OnTrip', 'UnderMaintenance'],
  columns: ['registrationNumber', 'model', 'year', 'capacity', 'currentOdometer', 'status'],
  fields: [
    { key: 'registrationNumber', label: 'Registration number', required: true, maxLength: 32 },
    { key: 'model', label: 'Model', required: true, maxLength: 120 },
    { key: 'year', label: 'Year', required: true, type: 'number', min: 1900, max: 2100, step: 1 },
    { key: 'capacity', label: 'Capacity (kg)', required: true, type: 'number', min: 0.01, max: 9999999999.99, step: 0.01 },
    { key: 'currentOdometer', label: 'Current odometer (km)', required: true, type: 'number', min: 0, max: Number.MAX_SAFE_INTEGER, step: 1 },
  ],
};
export const drivers: MasterConfig = {
  endpoint: 'drivers', title: 'Drivers', singular: 'Driver', nameKey: 'name',
  description: 'Manage driver details and availability. Manage login links separately in Users.',
  statuses: ['Available', 'OnTrip', 'Inactive'], columns: ['name', 'licenceNumber', 'contact', 'status'],
  fields: [
    { key: 'name', label: 'Name', required: true, maxLength: 160 },
    { key: 'licenceNumber', label: 'Licence number', required: true, maxLength: 64 },
    { key: 'contact', label: 'Contact', required: true, maxLength: 40 },
    { key: 'status', label: 'Status', required: true, options: ['Available', 'Inactive'] },
  ],
};
export const customers: MasterConfig = {
  endpoint: 'customers', title: 'Customer Companies', singular: 'Customer', nameKey: 'companyName',
  description: 'Maintain customer company and contact details for future order management.',
  columns: ['companyName', 'contactPerson', 'telephone', 'email'],
  fields: [
    { key: 'companyName', label: 'Company name', required: true, maxLength: 200 },
    { key: 'contactPerson', label: 'Contact person', maxLength: 160 },
    { key: 'telephone', label: 'Telephone', maxLength: 40 },
    { key: 'email', label: 'Email', type: 'email', maxLength: 254 },
    { key: 'address', label: 'Address', maxLength: 500 },
  ],
};
