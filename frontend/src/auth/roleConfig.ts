import type { Role } from './types';

interface NavigationItem { label: string; path?: string }
interface RoleConfig { label: string; color: string; introduction: string; navigation: NavigationItem[] }
export const roleConfig: Record<Role, RoleConfig> = {
  FleetAdministrator: {
    label: 'Fleet Administrator', color: '#126b69', introduction: 'Manage access and prepare your fleet records.',
    navigation: [{ label: 'Dashboard', path: '/' }, { label: 'Users', path: '/users' }, { label: 'Vehicles' }, { label: 'Drivers' }, { label: 'Customers' }],
  },
  OperationsCoordinator: {
    label: 'Operations Coordinator', color: '#285fa6', introduction: 'Your workspace for coordinating transport operations.',
    navigation: [{ label: 'Dashboard', path: '/' }, { label: 'Orders' }, { label: 'Trips' }],
  },
  Mechanic: {
    label: 'Mechanic', color: '#9a5d13', introduction: 'Your workspace for vehicle care and service follow-up.',
    navigation: [{ label: 'Dashboard', path: '/' }, { label: 'Maintenance' }, { label: 'Service Schedule' }, { label: 'Driver Notes' }],
  },
  Driver: {
    label: 'Driver', color: '#6344a1', introduction: 'Your personal workspace for assigned journeys.',
    navigation: [{ label: 'Dashboard', path: '/' }, { label: 'My Trips' }],
  },
  FleetOwner: {
    label: 'Fleet Owner', color: '#993f66', introduction: 'Your read-only workspace for the fleet overview.',
    navigation: [{ label: 'Dashboard', path: '/' }, { label: 'Reports' }],
  },
};
